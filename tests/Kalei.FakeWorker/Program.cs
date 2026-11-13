using System.IO.Pipes;
using Kalei.FakeWorker;

// Usage: Kalei.FakeWorker <behaviour> <handle the Worker reads> <handle the Worker writes>. Test-only, never shipped.
if (args.Length != 3)
{
    return 2;
}

using var fromCore = new AnonymousPipeClientStream(PipeDirection.In, args[1]);
using var toCore = new AnonymousPipeClientStream(PipeDirection.Out, args[2]);
try
{
    await Behaviours.RunAsync(args[0], fromCore, toCore, CancellationToken.None);
}
catch (IOException)
{
    // The Core closed the pipe: nothing more to say.
}

return args[0] == "NonZeroExitAfterResult" ? 1 : 0;
