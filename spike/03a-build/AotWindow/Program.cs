using Avalonia;
using Avalonia.Media;

namespace AotWindow;

internal static class Program
{
#if VARIANT_B
    public const string VariantName = "b";
#elif VARIANT_C
    public const string VariantName = "c";
#elif VARIANT_D
    public const string VariantName = "d";
#elif VARIANT_E
    public const string VariantName = "e";
#elif VARIANT_F
    public const string VariantName = "f";
#else
    public const string VariantName = "a";
#endif

    [STAThread]
    public static int Main(string[] args)
    {
        StartupMarks.Mark("main");
        var builder = BuildAvaloniaApp()
            .AfterPlatformServicesSetup(_ => StartupMarks.Mark("platformServices"))
            .AfterSetup(_ => StartupMarks.Mark("afterSetup"));
        StartupMarks.Mark("builderReady");
        return builder.StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
#if LEAN
        // c, f: Avalonia.Desktop (and its platform probing) is not used: only Win32, Skia and HarfBuzz (without UseHarfBuzz()
        // Avalonia throws "No text shaping system configured"); default font named explicitly.
        return AppBuilder.Configure<App>()
            .UseWin32()
            .UseSkia()
            .UseHarfBuzz()
            .With(new FontManagerOptions { DefaultFamilyName = "Segoe UI" });
#elif VARIANT_E
        // e: software rendering, no ANGLE/EGL (diagnostic: how much does GPU initialisation weigh on a VM without a GPU).
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions { RenderingMode = new[] { Win32RenderingMode.Software } });
#else
        return AppBuilder.Configure<App>().UsePlatformDetect();
#endif
    }
}
