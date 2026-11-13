using Kalei.Core.Ipc;

namespace Kalei.Worker.Builtin;

/// <summary>
/// The Worker's side of docs/ipc-spec.md: Hello first (before any engine is touched), one request, heartbeats while it runs,
/// the final frame. It distrusts the Core too: any frame that breaks the protocol ends it with code 2, unanswered.
/// </summary>
internal sealed class WorkerHost(Stream fromCore, Stream toCore, IRequestHandler handler, TimeSpan heartbeatInterval)
{
    public const int Done = 0;
    public const int ProtocolError = 2;

    private static readonly string s_workerId = "builtin-" + (typeof(WorkerHost).Assembly.GetName().Version?.ToString() ?? "0.0.0");

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var reader = new FrameReader(fromCore, Direction.ToWorker);
        try
        {
            await FrameWriter.WriteAsync(toCore, new HelloMessage { Protocol = IpcLimits.ProtocolVersion, Worker = s_workerId, Modules = BuiltinModules.All }, cancellationToken)
                .ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false) is not { Message: HelloMessage { Protocol: IpcLimits.ProtocolVersion } })
            {
                return ProtocolError; // another version, another message, or the Core went away
            }

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false) is not { Message: var request }
                || request is not (ProbeMessage or PreviewMessage or ConvertMessage or CanaryMessage))
            {
                return ProtocolError;
            }

            return await ServeAsync(reader, request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IpcException or IOException or ObjectDisposedException)
        {
            return ProtocolError;
        }
    }

    private async Task<int> ServeAsync(FrameReader reader, IpcMessage request, CancellationToken cancellationToken)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var next = reader.ReadAsync(stop.Token); // the only frame allowed from now on is Cancel
        var work = handler.HandleAsync(request, stop.Token);
        var heartbeats = HeartbeatsAsync(stop.Token);

        var first = await Task.WhenAny(work, next).ConfigureAwait(false);
        await stop.CancelAsync().ConfigureAwait(false);
        await heartbeats.ConfigureAwait(false); // nothing else writes while the final frame goes out

        if (first == next)
        {
            // Let the engine unwind (it was told to stop), but not for long: the Core kills this process after 3 s anyway.
            await Task.WhenAny(work, Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None)).ConfigureAwait(false);
            _ = work.ContinueWith(static t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            return await next.ConfigureAwait(false) is { Message: CancelMessage } ? Done : ProtocolError;
        }

        await FrameWriter.WriteAsync(toCore, await work.ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        return Done;
    }

    /// <summary>A pure heartbeat at a steady pace: at least every 2 s and at most 10 per second (spec, "Watchdog").</summary>
    private async Task HeartbeatsAsync(CancellationToken stopped)
    {
        try
        {
            using var timer = new PeriodicTimer(heartbeatInterval);
            while (await timer.WaitForNextTickAsync(stopped).ConfigureAwait(false))
            {
                await FrameWriter.WriteAsync(toCore, new ProgressMessage(), stopped).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // asked to stop
        }
    }
}
