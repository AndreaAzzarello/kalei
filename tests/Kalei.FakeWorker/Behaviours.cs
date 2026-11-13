using System.Buffers.Binary;
using System.Text;
using Kalei.Core.Ipc;

namespace Kalei.FakeWorker;

/// <summary>
/// Scripted Workers for the IPC tests (ticket 07a): one honest behaviour and one for every way the spec says a Worker can go
/// wrong. The same code runs in-process (fast) and in the Kalei.FakeWorker process (real pipes). A behaviour returns when it is
/// done and the caller closes the pipes, or waits forever and is killed.
/// </summary>
internal static class Behaviours
{
    private static readonly HelloMessage s_hello = new()
    {
        Protocol = 1,
        Worker = "fake-1",
        Modules = [new ModuleInfo { Id = "images", Version = "1.0.0" }],
    };

    public static async Task RunAsync(string name, Stream fromCore, Stream toCore, CancellationToken ct)
    {
        var reader = new FrameReader(fromCore, Direction.ToWorker);

        // Behaviours of the handshake phase.
        switch (name)
        {
            case "Silent":
                await Task.Delay(Timeout.Infinite, ct);
                return;
            case "BadVersion":
                await FrameWriter.WriteAsync(toCore, s_hello with { Protocol = 2 }, ct);
                await Task.Delay(Timeout.Infinite, ct);
                return;
            case "FirstFrameIsProgress":
                await FrameWriter.WriteAsync(toCore, new ProgressMessage(), ct);
                await Task.Delay(Timeout.Infinite, ct);
                return;
            case "HelloBadModule":
                await FrameWriter.WriteAsync(toCore, s_hello with { Modules = [new ModuleInfo { Id = "ghost", Version = "9.9.9" }] }, ct);
                await Task.Delay(Timeout.Infinite, ct);
                return;
        }

        await FrameWriter.WriteAsync(toCore, s_hello, ct);
        if (await reader.ReadAsync(ct) is not { Message: HelloMessage })
        {
            return;
        }

        if (await reader.ReadAsync(ct) is not { Message: var request })
        {
            return;
        }

        switch (name)
        {
            case "Normal" or "NonZeroExitAfterResult":
                await AnswerAsync(toCore, request, ct);
                break;
            case "WorkerError":
                await FrameWriter.WriteAsync(toCore, new ErrorMessage { Code = ErrorCode.Corrupt }, ct);
                break;

            // Case 1, 2, 3: length.
            case "HugeLength":
                await RawAsync(toCore, Prefix(1_048_577), ct);
                break;
            case "NegativeLength":
                await RawAsync(toCore, Prefix(0xFFFFFFF0u), ct);
                break;
            case "ZeroLength":
                await RawAsync(toCore, Prefix(0), ct);
                break;
            case "TooLargeForType":
                await RawAsync(toCore, Frame("{\"type\":\"Progress\"" + new string(' ', 129 - 19) + "}"), ct);
                break;

            // Case 4: syntax.
            case "BadJsonPrefix":
                await RawAsync(toCore, Frame("{\"kind\":\"Progress\"}"), ct);
                break;
            case "InvalidJson":
                await RawAsync(toCore, Frame("{\"type\":\"Progress\",}"), ct);
                break;
            case "DuplicateKey":
                await RawAsync(toCore, Frame("{\"type\":\"Progress\",\"fraction\":0.1,\"fraction\":0.2}"), ct);
                break;
            case "NullValue":
                await RawAsync(toCore, Frame("{\"type\":\"Progress\",\"fraction\":null}"), ct);
                break;
            case "TrailingBytes":
                await RawAsync(toCore, Frame("{\"type\":\"Progress\"}x"), ct);
                break;

            // Case 5 to 8.
            case "UnknownType":
                await RawAsync(toCore, Frame("{\"type\":\"Bogus\",\"a\":1}"), ct);
                break;
            case "UnknownField":
                await RawAsync(toCore, Frame("{\"type\":\"Progress\",\"extra\":1}"), ct);
                break;
            case "MissingField":
                await RawAsync(toCore, Frame("{\"type\":\"Result\"}"), ct);
                break;
            case "BadValue":
                await RawAsync(toCore, Frame("{\"type\":\"Progress\",\"fraction\":2}"), ct);
                break;
            case "ForbiddenErrorCode":
                await FrameWriter.WriteAsync(toCore, new ErrorMessage { Code = ErrorCode.Protocol }, ct);
                break;
            case "ProgressBothFields":
                await RawAsync(toCore, Frame("{\"type\":\"Progress\",\"fraction\":0.5,\"bytes\":10}"), ct);
                break;
            case "ProgressDecreasing":
                await FrameWriter.WriteAsync(toCore, new ProgressMessage { Fraction = 0.5 }, ct);
                await FrameWriter.WriteAsync(toCore, new ProgressMessage { Fraction = 0.4 }, ct);
                break;
            case "ProgressBytesOverSource":
                await FrameWriter.WriteAsync(toCore, new ProgressMessage { Bytes = 1_000_000 }, ct);
                break;
            case "PreviewLengthMismatch":
                await RawAsync(toCore, [.. Frame("{\"type\":\"PreviewData\",\"width\":2,\"height\":2,\"length\":15}"), .. new byte[15]], ct);
                break;
            case "PreviewTooBig":
                await FrameWriter.WritePreviewDataAsync(toCore, 32, 32, new byte[32 * 32 * 4], ct);
                break;

            // Case 9: sequence.
            case "AlwaysProbeResult":
                await FrameWriter.WriteAsync(toCore, ProbeAnswer(), ct);
                break;
            case "TwoFinals":
                await FrameWriter.WriteAsync(toCore, new ResultMessage { OutputBytes = 10 }, ct);
                await FrameWriter.WriteAsync(toCore, new ResultMessage { OutputBytes = 10 }, ct);
                break;
            case "FrameAfterFinal":
                await FrameWriter.WriteAsync(toCore, new ResultMessage { OutputBytes = 10 }, ct);
                await FrameWriter.WriteAsync(toCore, new ProgressMessage(), ct);
                break;
            case "HelloInOpen":
                await FrameWriter.WriteAsync(toCore, s_hello, ct);
                break;

            // Case 11: rate.
            case "Flood":
                await RawAsync(toCore, Flood(150), ct);
                break;

            // Case 12: the Worker ends or the pipe closes.
            case "CloseMidFrame":
                await RawAsync(toCore, [.. Prefix(100), .. "{\"type\":\"Progress\""u8], ct);
                return;
            case "ExitBeforeFinal":
                return;

            // Case 13 and 14: silence, endless heartbeats, ignoring or obeying Cancel.
            case "StallAfterHello":
                break;
            case "Heartbeats":
                await HeartbeatsAsync(toCore, reader, obeyCancel: false, ct);
                return;
            case "CancelRespect":
                await HeartbeatsAsync(toCore, reader, obeyCancel: true, ct);
                return;
        }

        // Stay alive: the Core must end the Worker (kill), the Worker never leaves by itself after a violation.
        if (name is not ("Normal" or "WorkerError" or "NonZeroExitAfterResult"))
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
    }

    private static async Task AnswerAsync(Stream toCore, IpcMessage request, CancellationToken ct)
    {
        switch (request)
        {
            case ProbeMessage:
                await FrameWriter.WriteAsync(toCore, ProbeAnswer(), ct);
                break;
            case PreviewMessage:
                await FrameWriter.WritePreviewDataAsync(toCore, 2, 2, new byte[16], ct);
                break;
            case ConvertMessage:
                await FrameWriter.WriteAsync(toCore, new ProgressMessage { Fraction = 0.5 }, ct);
                await FrameWriter.WriteAsync(toCore, new ResultMessage { OutputBytes = 10, Warnings = [Notice.FirstFrameOnly] }, ct);
                break;
            case CanaryMessage:
                await FrameWriter.WriteAsync(toCore, new CanaryResultMessage
                {
                    Local = new CanaryProbe { Outcome = CanaryOutcome.Timeout },
                    Net = new CanaryProbe { Outcome = CanaryOutcome.Error, Code = 10013 },
                }, ct);
                break;
        }
    }

    private static ProbeResultMessage ProbeAnswer() => new()
    {
        Format = "png",
        Confidence = Confidence.Certain,
        Properties = new ProbeProperties { Width = 1, Height = 1 },
    };

    private static async Task HeartbeatsAsync(Stream toCore, FrameReader reader, bool obeyCancel, CancellationToken ct)
    {
        var next = reader.ReadAsync(ct);
        while (true)
        {
            await FrameWriter.WriteAsync(toCore, new ProgressMessage(), ct);
            await Task.WhenAny(next, Task.Delay(50, ct));
            if (next.IsCompleted)
            {
                if (obeyCancel && next.Result is { Message: CancelMessage })
                {
                    return;
                }

                next = reader.ReadAsync(ct); // a Worker that does not obey simply reads on
            }
        }
    }

    private static byte[] Prefix(uint length)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, length);
        return bytes;
    }

    private static byte[] Frame(string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        return [.. Prefix((uint)body.Length), .. body];
    }

    private static byte[] Flood(int count)
    {
        var one = Frame("{\"type\":\"Progress\"}");
        return [.. Enumerable.Range(0, count).SelectMany(_ => one)];
    }

    private static async Task RawAsync(Stream toCore, byte[] bytes, CancellationToken ct)
    {
        await toCore.WriteAsync(bytes, ct);
        await toCore.FlushAsync(ct);
    }
}
