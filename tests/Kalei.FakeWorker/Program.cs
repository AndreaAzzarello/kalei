using System.IO.Pipes;
using Kalei.Core.Ipc;
using Kalei.FakeWorker;

// Usage: Kalei.FakeWorker <behaviour> [extra arguments] <handle the Worker reads> <handle the Worker writes>. Test-only, never shipped.
// "probe:<name>" runs a probe of Probes.cs; "AllocateForever" and "SpinAfterHandshake" are the resource misbehaviours that only make
// sense in a real process (never run in-process: they would eat the memory or a core of the test run).
if (args.Length < 3)
{
    return 2;
}

var behaviour = args[0];
string[] extra = args[1..^2];
using var fromCore = new AnonymousPipeClientStream(PipeDirection.In, args[^2]);
using var toCore = new AnonymousPipeClientStream(PipeDirection.Out, args[^1]);
try
{
    if (behaviour.StartsWith("probe:", StringComparison.Ordinal))
    {
        await Probes.RunAsync(behaviour["probe:".Length..], extra, toCore);
    }
    else if (behaviour is "AllocateForever" or "SpinAfterHandshake")
    {
        // The honest part first: Hello, the Core's Hello and the request; then the misbehaviour.
        var reader = new FrameReader(fromCore, Direction.ToWorker);
        await FrameWriter.WriteAsync(toCore, Behaviours.Hello, CancellationToken.None);
        await reader.ReadAsync(CancellationToken.None);
        await reader.ReadAsync(CancellationToken.None);
        if (behaviour == "AllocateForever")
        {
            List<byte[]> kept = [];
            while (true)
            {
                var block = new byte[16 * 1024 * 1024];
                Array.Fill(block, (byte)1); // touch it: memory that is committed and used
                kept.Add(block);
            }
        }

        while (true)
        {
            // burn CPU, send nothing
        }
    }
    else
    {
        await Behaviours.RunAsync(behaviour, fromCore, toCore, CancellationToken.None);
    }
}
catch (IOException)
{
    // The Core closed the pipe: nothing more to say.
}
catch (OutOfMemoryException)
{
    return 3; // the memory cap was hit; the sandbox says why, not this exit code
}

return behaviour == "NonZeroExitAfterResult" ? 1 : 0;
