using Microsoft.Win32.SafeHandles;

namespace Kalei.Worker.Builtin;

internal static class Program
{
    /// <summary>Started by the Core with two arguments: the handle it reads from and the handle it writes to (inherited anonymous pipes).</summary>
    private static async Task<int> Main(string[] args)
    {
        if (args.Length != 2 || !long.TryParse(args[0], out var input) || !long.TryParse(args[1], out var output))
        {
            return WorkerHost.ProtocolError; // not started by Kalei: there is nobody to talk to
        }

        await using var fromCore = Open(input, FileAccess.Read);
        await using var toCore = Open(output, FileAccess.Write);
        return await new WorkerHost(fromCore, toCore, new UnavailableHandler(), TimeSpan.FromSeconds(1)).RunAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private static FileStream Open(long handle, FileAccess access) =>
        new(new SafeFileHandle(new IntPtr(handle), ownsHandle: true), access, bufferSize: 1, isAsync: false);
}
