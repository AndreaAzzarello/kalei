using Avalonia.Controls;
using Avalonia.Threading;
#if DEFERRED
using Avalonia;
using Avalonia.Media;
#endif

namespace AotWindow;

/// <summary>
/// Start-up measure: from the kernel creation time of the process to the callback of the SECOND animation frame after the
/// window opened (the first frame is then on screen) with the UI thread idle and accepting input: mark "frame2" (the W7 metric).
/// Other marks: see StartupMarks. With "--out file" one JSON line is written and the app exits, so a script can run it many times.
/// Variants (03c): DEFERRED = minimal window first, full view after frame2; LEAN = ComboBox filled after frame2.
/// For these two, "fullContent" is the frame after the complete content is shown (the user's real "ready"); for the others the
/// complete content is already there at frame2 and the mark is absent.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly string[] Formats = { "PNG", "JPG", "WebP", "AVIF", "TIFF", "GIF" };
    private bool _frame2Seen;
    private bool _layoutSeen;
    private bool _finished;

    public MainWindow()
    {
#if DEFERRED
        Title = "Kalei AOT spike";
        Width = 640;
        Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = BuildMinimal();
#else
        InitializeComponent();
#if !LEAN
        Targets.ItemsSource = Formats;
#endif
#endif
        LayoutUpdated += (_, _) =>
        {
            if (_layoutSeen) return;
            _layoutSeen = true;
            StartupMarks.Mark("firstLayout");
        };
        Opened += (_, _) =>
        {
            StartupMarks.Mark("opened");
            RequestAnimationFrame(_ =>
            {
                StartupMarks.Mark("frame1");
                RequestAnimationFrame(_ => Dispatcher.UIThread.Post(Measure, DispatcherPriority.Input));
            });
        };
        StartupMarks.Mark("windowCtorEnd");
    }

#if DEFERRED
    // Same look as MainWindow's grid (logotype and the drop border), built in code: no AXAML parsing before the first frame.
    private static Control BuildMinimal()
    {
        var grid = new Grid { Margin = new Thickness(24), RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        grid.Children.Add(new TextBlock { Text = "kalei", FontSize = 36, FontWeight = FontWeight.SemiBold });
        var border = new Border { Margin = new Thickness(0, 16), CornerRadius = new CornerRadius(16), BorderThickness = new Thickness(2), BorderBrush = new SolidColorBrush(Color.Parse("#6C5FF1")) };
        Grid.SetRow(border, 1);
        grid.Children.Add(border);
        return grid;
    }
#endif

    private void Measure()
    {
        if (_frame2Seen) return;
        _frame2Seen = true;
        StartupMarks.Mark("frame2");
#if DEFERRED
        var full = new FullView();
        full.Targets.ItemsSource = Formats;
        Content = full;
        AfterFullContent();
#elif LEAN
        Targets.ItemsSource = Formats;
        AfterFullContent();
#else
        Finish();
#endif
    }

#if DEFERRED || LEAN
    private void AfterFullContent() =>
        RequestAnimationFrame(_ => RequestAnimationFrame(_ =>
        {
            StartupMarks.Mark("fullContent");
            Finish();
        }));
#endif

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        var json = StartupMarks.Report(Program.VariantName);
        var args = Environment.GetCommandLineArgs();
        var i = Array.IndexOf(args, "--out");
        if (i >= 0 && i + 1 < args.Length)
        {
            File.WriteAllText(args[i + 1], json);
            Close();
        }
        else Title = json;
    }
}
