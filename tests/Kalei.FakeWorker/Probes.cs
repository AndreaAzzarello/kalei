using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Kalei.FakeWorker;

/// <summary>
/// Things a Worker must NOT be able to do inside the sandbox (ticket 07b), tried from inside a real Worker process. Each probe
/// answers one line: "ok ..." if the operation worked, "blocked ..." if it failed. Only error kinds are written, never a path.
/// The tests run every probe outside the sandbox first (it must say "ok": the positive control) and then inside (it must say "blocked").
/// </summary>
internal static class Probes
{
    private static readonly TimeSpan s_connectTimeout = TimeSpan.FromSeconds(3);

    public static async Task RunAsync(string name, string[] extra, Stream toCore)
    {
        string line;
        try
        {
            line = await TryAsync(name, extra);
        }
        catch (SocketException e)
        {
            line = $"blocked {e.SocketErrorCode}";
        }
        catch (Exception e)
        {
            line = $"blocked {e.GetType().Name}";
        }

        await toCore.WriteAsync(Encoding.UTF8.GetBytes(line + "\n"));
        await toCore.FlushAsync();
        if (name is "pid-wait")
        {
            await Task.Delay(Timeout.Infinite);
        }
    }

    /// <summary>Paths travel as hex so no space or quote can break the command line.</summary>
    public static string Decode(string hex) => Encoding.UTF8.GetString(Convert.FromHexString(hex));

    private static async Task<string> TryAsync(string name, string[] extra)
    {
        switch (name)
        {
            case "tcp":
                {
                    using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    using var timeout = new CancellationTokenSource(s_connectTimeout);
                    try
                    {
                        await socket.ConnectAsync(IPAddress.Parse(extra[0]), int.Parse(extra[1], System.Globalization.CultureInfo.InvariantCulture), timeout.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return "blocked timeout";
                    }

                    return "ok connected";
                }

            case "udp-dns":
                {
                    using var udp = new UdpClient();
                    udp.Client.ReceiveTimeout = (int)s_connectTimeout.TotalMilliseconds;
                    byte[] query = [0x12, 0x34, 0x01, 0x00, 0, 1, 0, 0, 0, 0, 0, 0, 7, .. "example"u8, 3, .. "com"u8, 0, 0, 1, 0, 1];
                    await udp.SendAsync(query, new IPEndPoint(IPAddress.Parse(extra[0]), 53));
                    using var timeout = new CancellationTokenSource(s_connectTimeout);
                    try
                    {
                        var answer = await udp.ReceiveAsync(timeout.Token);
                        return answer.Buffer.Length > 0 ? "ok answered" : "blocked empty";
                    }
                    catch (OperationCanceledException)
                    {
                        return "blocked no answer";
                    }
                }

            case "dns":
                {
                    var addresses = await Dns.GetHostAddressesAsync("example.com");
                    return addresses.Length > 0 ? "ok resolved" : "blocked none";
                }

            case "read-file":
                return $"ok {File.ReadAllBytes(Decode(extra[0])).Length} bytes";

            case "list-dir":
                return $"ok {Directory.GetFileSystemEntries(Decode(extra[0])).Length} entries";

            case "write-dir":
                {
                    var file = Path.Combine(Decode(extra[0]), $"kalei-probe-{Guid.NewGuid():N}.tmp");
                    File.WriteAllText(file, "x");
                    File.Delete(file);
                    return "ok written";
                }

            case "read-handle":
                {
                    using var stream = new FileStream(new SafeFileHandle((nint)long.Parse(extra[0], System.Globalization.CultureInfo.InvariantCulture), ownsHandle: false), FileAccess.Read, 1, isAsync: false);
                    stream.Seek(6, SeekOrigin.Begin); // seekable, as ADR 0003 requires
                    var buffer = new byte[14];
                    var read = stream.Read(buffer, 0, buffer.Length);
                    return "ok " + Encoding.UTF8.GetString(buffer, 0, read);
                }

            case "child":
                {
                    using var child = Process.Start(new ProcessStartInfo("cmd.exe", "/c exit 0") { UseShellExecute = false, CreateNoWindow = true })
                        ?? throw new InvalidOperationException("no process");
                    child.WaitForExit();
                    return "ok started";
                }

            case "env":
                return Environment.GetEnvironmentVariable("USERNAME") is null && Environment.GetEnvironmentVariable("USERPROFILE") is null
                    ? "blocked no user variables"
                    : "ok user variables present";

            case "pid-wait":
                return $"ok {Environment.ProcessId}";

            case "spin":
                while (true)
                {
                    // burn CPU until the sandbox ends the process
                }

            default:
                throw new ArgumentException("unknown probe", nameof(name));
        }
    }
}
