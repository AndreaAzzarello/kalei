using Avalonia;

namespace DonutSpike;

/// <summary>Command line: --exit-after-drop (cold-run mode: one drag per process), --log &lt;file&gt;, --tag &lt;text&gt;, --screen &lt;n&gt; (open centred on screen n), --straddle &lt;n&gt; (centre of the window on the right edge of screen n).</summary>
public sealed record Options(bool ExitAfterDrop, string LogPath, string Tag, int? Screen = null, bool Straddle = false);

internal static class Program
{
    public static Options Options { get; private set; } = new(false, DefaultLog(), "");

    [STAThread]
    public static int Main(string[] args)
    {
        var exit = args.Contains("--exit-after-drop");
        var log = ValueOf(args, "--log") ?? DefaultLog();
        var tag = ValueOf(args, "--tag") ?? "";
        int? screen = int.TryParse(ValueOf(args, "--screen") ?? ValueOf(args, "--straddle"), out var s) ? s : null;
        Options = new Options(exit, log, tag, screen, args.Contains("--straddle"));
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect();

    private static string? ValueOf(string[] a, string key)
    {
        var i = Array.IndexOf(a, key);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
    }

    private static string DefaultLog() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KaleiSpike04", "latency-v2.csv");
}
