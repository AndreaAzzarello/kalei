using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AotWindow;

/// <summary>
/// Start-up measure: from the kernel creation time of the process to the callback of the SECOND animation frame after the
/// window opened (the first frame is then on screen) with the UI thread idle and accepting input. With "--out file" the value
/// in milliseconds is written to the file and the app exits, so a script can run it many times.
/// </summary>
public partial class MainWindow : Window
{
    private bool _done;

    public MainWindow()
    {
        InitializeComponent();
        Targets.ItemsSource = new[] { "PNG", "JPG", "WebP", "AVIF", "TIFF", "GIF" };
        Opened += (_, _) => RequestAnimationFrame(_ => RequestAnimationFrame(_ => Dispatcher.UIThread.Post(Measure, DispatcherPriority.Input)));
    }

    private void Measure()
    {
        if (_done) return;
        _done = true;
        var started = Process.GetCurrentProcess().StartTime.ToUniversalTime();
        var ms = (DateTime.UtcNow - started).TotalMilliseconds;
        var args = Environment.GetCommandLineArgs();
        var i = Array.IndexOf(args, "--out");
        if (i >= 0 && i + 1 < args.Length)
        {
            File.WriteAllText(args[i + 1], ms.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
            Close();
        }
        else Info.Text = $"First frame after {ms:F0} ms";
    }
}
