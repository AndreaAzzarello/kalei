// THROWAWAY spike worker (ticket 01). It receives numeric handle values, never paths for Source/Output.
// Probe modes that need a path (fs-probe) get it from the launcher on purpose: they test that access is DENIED.
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;
using NetVips;

namespace SandboxSpike.Worker;

internal static class Program
{
    private static int Main(string[] argv)
    {
        var a = Args.Parse(argv);
        var pipe = new FileStream(new SafeFileHandle(new IntPtr(a.Long("pipe")), true), FileAccess.Write, 1, false);
        var writer = new StreamWriter(pipe) { AutoFlush = true };
        writer.WriteLine("HELLO"); // first byte seen by the launcher: start-up latency
        var sw = Stopwatch.StartNew();
        Dictionary<string, object?> result;
        try
        {
            result = a.Mode switch
            {
                "info" => Info(),
                "convert" => Convert(a),
                "read" => Read(a),
                "net-tcp" => NetTcp(a),
                "net-udp" => NetUdp(),
                "net-dns" => NetDns(),
                "net-loopback" => NetLoopback(a),
                "fs-probe" => FsProbe(a),
                "alloc" => Alloc(),
                "hang" => Hang(),
                "spawn-child" => SpawnChild(),
                _ => Fail("unknown mode " + a.Mode),
            };
        }
        catch (Exception e)
        {
            result = Fail(e.GetType().Name + ": " + e.Message);
        }
        result["mode"] = a.Mode;
        result["ms"] = sw.ElapsedMilliseconds;
        writer.WriteLine("RESULT " + JsonSerializer.Serialize(result));
        return 0;
    }

    private static Dictionary<string, object?> Ok(params (string, object?)[] kv)
    {
        var d = new Dictionary<string, object?> { ["ok"] = true };
        foreach (var (k, v) in kv) d[k] = v;
        return d;
    }

    private static Dictionary<string, object?> Fail(string error) =>
        new() { ["ok"] = false, ["error"] = error };

    private static FileStream Open(long h, FileAccess access) =>
        new(new SafeFileHandle(new IntPtr(h), false), access, 1, false);

    private static Dictionary<string, object?> Info() =>
        Ok(("libvips", NetVips.NetVips.Version(0) + "." + NetVips.NetVips.Version(1) + "." + NetVips.NetVips.Version(2)),
           ("runtime", Environment.Version.ToString()));

    // W1: read a PNG through an inherited handle, write AVIF or WebP to an inherited handle, read the output back and decode pixels.
    private static Dictionary<string, object?> Convert(Args a)
    {
        var fmt = a.Str("fmt");
        using var input = Open(a.Long("in"), FileAccess.Read);
        using var output = Open(a.Long("out"), FileAccess.ReadWrite);
        var sw = Stopwatch.StartNew();
        using (var img = Image.NewFromStream(input))
        {
            img.WriteToStream(output, "." + fmt);
        }
        output.Flush();
        var convertMs = sw.ElapsedMilliseconds;
        var length = output.Length;
        output.Position = 0;
        using var back = Image.NewFromStream(output);
        var avg = back.Avg(); // forces a pixel decode
        return Ok(("convertMs", convertMs), ("outBytes", length), ("width", back.Width), ("height", back.Height), ("avg", avg));
    }

    // W3: random access reads through a handle; counts the seeks libvips performs.
    private static Dictionary<string, object?> Read(Args a)
    {
        using var raw = Open(a.Long("in"), FileAccess.Read);
        var counting = new CountingStream(raw);
        var pages = a.Has("pages") ? (int)a.Long("pages") : 1;
        var regions = 0;
        double sum = 0;
        var dims = new List<string>();
        for (var p = pages - 1; p >= 0; p--) // deepest level first, then back: not sequential
        {
            raw.Position = 0; // a new image sniffs the format from the current position of the stream
            using var img = Image.NewFromStream(counting,pages > 1 ? $"[page={p}]" : "", Enums.Access.Random);
            dims.Add($"{img.Width}x{img.Height}");
            foreach (var (fx, fy) in new[] { (0.9, 0.9), (0.1, 0.1), (0.5, 0.2), (0.2, 0.8) })
            {
                var w = Math.Min(64, img.Width);
                var h = Math.Min(64, img.Height);
                var x = Math.Clamp((int)(img.Width * fx), 0, img.Width - w);
                var y = Math.Clamp((int)(img.Height * fy), 0, img.Height - h);
                using var tile = img.Crop(x, y, w, h);
                sum += tile.Avg();
                regions++;
            }
        }
        return Ok(("canSeek", raw.CanSeek), ("seeks", counting.Seeks), ("regions", regions), ("sum", sum), ("levels", string.Join(",", dims)));
    }

    // W2: network probes. ok == true means the network WORKED (bad inside the sandbox).
    private static Dictionary<string, object?> NetTcp(Args a)
    {
        var host = a.Str("host");
        var port = (int)a.Long("port");
        using var client = new TcpClient();
        var task = client.ConnectAsync(IPAddress.Parse(host), port);
        if (!task.Wait(5000)) return Fail("timeout");
        return Ok(("connected", client.Connected));
    }

    private static Dictionary<string, object?> NetUdp()
    {
        // Raw DNS query for example.com A, sent to 1.1.1.1:53; success only if an answer comes back.
        byte[] q = [0x12, 0x34, 0x01, 0x00, 0, 1, 0, 0, 0, 0, 0, 0,
            7, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e',
            3, (byte)'c', (byte)'o', (byte)'m', 0, 0, 1, 0, 1];
        using var udp = new UdpClient();
        udp.Client.ReceiveTimeout = 4000;
        udp.Send(q, q.Length, new IPEndPoint(IPAddress.Parse("1.1.1.1"), 53));
        IPEndPoint? from = null;
        var answer = udp.Receive(ref from);
        return Ok(("answerBytes", answer.Length));
    }

    private static Dictionary<string, object?> NetDns()
    {
        var addrs = Dns.GetHostAddresses("example.com");
        return Ok(("addresses", addrs.Length));
    }

    private static Dictionary<string, object?> NetLoopback(Args a) => NetTcp(a);

    // Filesystem access by PATH, outside the handles. ok == true means the access WORKED (bad inside the sandbox).
    private static Dictionary<string, object?> FsProbe(Args a)
    {
        var results = new List<Dictionary<string, object?>>();
        foreach (var spec in a.All("probe"))
        {
            var i = spec.IndexOf('=');
            var kind = spec[..i];
            var path = spec[(i + 1)..];
            string? error = null;
            var allowed = false;
            try
            {
                switch (kind)
                {
                    case "readfile": _ = File.ReadAllBytes(path); allowed = true; break;
                    case "listdir": _ = Directory.GetFileSystemEntries(path); allowed = true; break;
                    case "writefile":
                        var f = Path.Combine(path, "kalei-spike-write.tmp");
                        File.WriteAllText(f, "x");
                        File.Delete(f);
                        allowed = true;
                        break;
                }
            }
            catch (Exception e) { error = e.GetType().Name; }
            results.Add(new() { ["kind"] = kind, ["name"] = Path.GetFileName(path.TrimEnd('\\')), ["allowed"] = allowed, ["error"] = error });
        }
        return Ok(("probes", results));
    }

    // Job Object proofs.
    private static Dictionary<string, object?> Alloc()
    {
        var chunks = new List<byte[]>();
        const int chunkMb = 64;
        try
        {
            for (var i = 0; i < 64; i++) // up to 4 GB
            {
                var b = new byte[chunkMb * 1024 * 1024];
                Array.Fill(b, (byte)1); // touch the pages
                chunks.Add(b);
            }
        }
        catch (OutOfMemoryException) { return Ok(("allocatedMb", chunks.Count * chunkMb), ("stoppedBy", "OutOfMemoryException")); }
        return Ok(("allocatedMb", chunks.Count * chunkMb), ("stoppedBy", "cap"));
    }

    private static Dictionary<string, object?> Hang()
    {
        Thread.Sleep(Timeout.Infinite);
        return Ok();
    }

    private static Dictionary<string, object?> SpawnChild()
    {
        var p = Process.Start(new ProcessStartInfo("cmd.exe", "/c exit 0") { CreateNoWindow = true, UseShellExecute = false });
        p!.WaitForExit(3000);
        return Ok(("started", true));
    }
}

internal sealed class Args
{
    private readonly Dictionary<string, List<string>> _v = new();
    public string Mode { get; private set; } = "";

    public static Args Parse(string[] argv)
    {
        var a = new Args();
        for (var i = 0; i < argv.Length; i++)
        {
            if (i == 0) { a.Mode = argv[0]; continue; }
            if (!argv[i].StartsWith("--")) continue;
            var key = argv[i][2..];
            var val = i + 1 < argv.Length ? argv[++i] : "";
            if (!a._v.TryGetValue(key, out var list)) a._v[key] = list = [];
            list.Add(val);
        }
        return a;
    }

    public bool Has(string k) => _v.ContainsKey(k);
    public string Str(string k) => _v[k][0];
    public long Long(string k) => long.Parse(_v[k][0]);
    public IEnumerable<string> All(string k) => _v.TryGetValue(k, out var l) ? l : [];
}

internal sealed class CountingStream(Stream inner) : Stream
{
    public int Seeks { get; private set; }
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => inner.Length;
    public override long Position { get => inner.Position; set { Seeks++; inner.Position = value; } }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) { Seeks++; return inner.Seek(offset, origin); }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
