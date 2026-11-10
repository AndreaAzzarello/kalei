using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DonutSpike.Logic;

namespace DonutSpike;

/// <summary>
/// W10 spike: an Avalonia window that opens a radial overlay when files are dragged over it, and logs
/// how long that takes. The logs hold only extensions, formats, sizes and timings, never file names or paths.
/// </summary>
public partial class MainWindow : Window
{
    private const double RingDiameter = 260;
    private const double EdgeMargin = 8;

    private readonly Options _opt;
    private readonly string _runId = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
    private int _seq;
    private bool _overlayOn;
    private Border? _ring;
    private (long Size, DateTime Mtime, string? Path)? _firstFile;
    private static readonly IBrush Surface = new SolidColorBrush(Color.Parse("#F2F2F7"));
    private static readonly IBrush Line = new SolidColorBrush(Color.Parse("#8A8A99"));
    private static readonly IBrush Text = new SolidColorBrush(Color.Parse("#1B1B22"));

    public MainWindow() : this(new Options(false, Path.Combine(Path.GetTempPath(), "donut-spike.csv"), "")) { }

    public MainWindow(Options opt)
    {
        _opt = opt;
        InitializeComponent();
        DragDrop.SetAllowDrop(this, true);
        DragDrop.AddDragEnterHandler(this, OnDragEnter);
        DragDrop.AddDragOverHandler(this, OnDragOver);
        DragDrop.AddDragLeaveHandler(this, OnDragLeave);
        DragDrop.AddDropHandler(this, OnDrop);
        if (_opt.Screen is not null)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Opened += (_, _) => MoveToRequestedScreen();
        }
        Info.Text = $"run {_runId}  log {Path.GetFileName(_opt.LogPath)}  tag '{_opt.Tag}'";
    }

    private void MoveToRequestedScreen()
    {
        var all = Screens.All;
        if (_opt.Screen is not { } n || n < 0 || n >= all.Count) return;
        var b = all[n].Bounds;
        var w = (int)(Bounds.Width * RenderScaling);
        var h = (int)(Bounds.Height * RenderScaling);
        Position = _opt.Straddle
            ? new PixelPoint(b.Right - w / 2, b.Y + Math.Max(0, (b.Height - h) / 2))
            : new PixelPoint(b.X + Math.Max(0, (b.Width - w) / 2), b.Y + Math.Max(0, (b.Height - h) / 2));
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        var t0 = Stopwatch.GetTimestamp(); // first line: the first drag-enter received by the window
        e.DragEffects = DragDropEffects.Copy;
        if (_overlayOn) return; // child enter/leave churn: measure only the first one of a drag
        _overlayOn = true;
        _seq++;

        var files = e.DataTransfer.TryGetFiles() ?? [];
        var tNames = Stopwatch.GetTimestamp();
        var names = files.Select(f => f.Name).ToList();
        var hints = names.Select(FormatHint.FromName).ToList();
        var targets = Targets.For(hints).ToList();
        var tExt = Stopwatch.GetTimestamp();

        var pos = e.GetPosition(this);
        ShowOverlay(pos, targets, hints.Count == 0 ? "?" : hints.All(h => h.Known) ? string.Join("+", hints.Select(h => h.Format).Distinct()) : "analysing");
        var tShown = Stopwatch.GetTimestamp();

        double t1 = double.NaN, t2 = double.NaN;
        var done = false;
        RequestAnimationFrame(_ =>
        {
            t1 = LatencyStats.ToMs(Stopwatch.GetTimestamp() - t0);
            RequestAnimationFrame(_ =>
            {
                t2 = LatencyStats.ToMs(Stopwatch.GetTimestamp() - t0);
                done = true;
            });
        });

        // Header sniff of the first file, after the overlay is up and timed apart (S3: headers only, 16 bytes).
        var tH0 = Stopwatch.GetTimestamp();
        string headerFormat = "-";
        var first = files.FirstOrDefault();
        if (first?.TryGetLocalPath() is { } p && File.Exists(p))
        {
            try
            {
                var fi = new FileInfo(p);
                _firstFile = (fi.Length, fi.LastWriteTimeUtc, p);
                Span<byte> buf = stackalloc byte[16];
                using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var n = fs.Read(buf);
                    headerFormat = FormatHint.FromHeader(buf[..n], p).Format;
                }
            }
            catch (Exception ex) { headerFormat = "error:" + ex.GetType().Name; }
        }
        var headerMs = LatencyStats.ToMs(Stopwatch.GetTimestamp() - tH0);

        // Watchdog: if no frame arrives within 1 s the render loop is starved during the drag; record that.
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Append(_opt.LogPath, "latency", done ? null : "NO_FRAME_IN_1S", pos, files.Length, hints, headerFormat,
                LatencyStats.ToMs(tNames - t0), LatencyStats.ToMs(tExt - t0), LatencyStats.ToMs(tShown - t0), t1, t2, headerMs);
            Status.Text = $"enter #{_seq}: overlay {t1:F1} ms, frame {t2:F1} ms, formats {string.Join("+", hints.Select(h => h.Format).Distinct())}, header {headerFormat}";
        };
        timer.Start();
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        if (_overlayOn && _ring is not null)
            Place(e.GetPosition(this));
    }

    private void OnDragLeave(object? sender, RoutedEventArgs e) => HideOverlay();

    private void OnDrop(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        var effectReported = e.DragEffects;
        HideOverlay();
        var first = _firstFile;
        _ = Task.Run(async () =>
        {
            string before = "-", after = "-"; bool sizeSame = false, mtimeSame = false;
            if (first is { Path: { } p } f)
            {
                try
                {
                    before = Sha(p);
                    await Task.Delay(500);
                    after = Sha(p);
                    var fi = new FileInfo(p);
                    sizeSame = fi.Exists && fi.Length == f.Size;
                    mtimeSame = fi.Exists && fi.LastWriteTimeUtc == f.Mtime;
                }
                catch (Exception ex) { before = after = "error:" + ex.GetType().Name; }
            }
            var unchanged = before == after && before != "-" && sizeSame && mtimeSame;
            File.AppendAllText(Path.ChangeExtension(_opt.LogPath, ".drops.csv"),
                $"{_runId},{_seq},{_opt.Tag},{effectReported},{unchanged},{sizeSame},{mtimeSame}{Environment.NewLine}");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Status.Text = $"drop #{_seq}: effect {effectReported}, source unchanged: {unchanged}";
                if (_opt.ExitAfterDrop) Close();
            });
        });
        _firstFile = null;
    }

    private static string Sha(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Convert.ToHexString(SHA256.HashData(fs));
    }

    private void ShowOverlay(Point cursor, IReadOnlyList<string> targets, string hubText)
    {
        Overlay.Children.Clear();
        var ring = new Border { Width = RingDiameter, Height = RingDiameter, Background = Brushes.Transparent };
        var grid = new Canvas { Width = RingDiameter, Height = RingDiameter };
        var c = RingDiameter / 2;
        grid.Children.Add(Ellipse2(c, c, c - 2, Line));
        var hub = Label(hubText, 72, 72, 36);
        Canvas.SetLeft(hub, c - 36); Canvas.SetTop(hub, c - 36);
        grid.Children.Add(hub);
        var centres = DonutGeometry.SliceCentres(targets.Count, 90);
        for (var i = 0; i < targets.Count; i++)
        {
            var s = Label(targets[i].ToUpperInvariant(), 64, 40, 12);
            Canvas.SetLeft(s, c + centres[i].X - 32);
            Canvas.SetTop(s, c + centres[i].Y - 20);
            grid.Children.Add(s);
        }
        ring.Child = grid;
        _ring = ring;
        Overlay.Children.Add(ring);
        Overlay.IsVisible = true;
        Place(cursor);
    }

    private void Place(Point cursor)
    {
        if (_ring is null) return;
        var (l, t) = DonutGeometry.Place(cursor.X, cursor.Y, RingDiameter, Bounds.Width, Bounds.Height, EdgeMargin);
        Canvas.SetLeft(_ring, l);
        Canvas.SetTop(_ring, t);
    }

    private void HideOverlay()
    {
        _overlayOn = false;
        Overlay.IsVisible = false;
        Overlay.Children.Clear();
        _ring = null;
    }

    private static Border Label(string text, double w, double h, double radius) => new()
    {
        Width = w, Height = h, CornerRadius = new CornerRadius(radius), Background = Surface,
        BorderBrush = Line, BorderThickness = new Thickness(1),
        Child = new TextBlock { Text = text, Foreground = Text, FontSize = 12, FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
    };

    // Ring outline as an Ellipse-shaped Border (keeps the overlay to a few simple visuals).
    private static Border Ellipse2(double cx, double cy, double r, IBrush stroke)
    {
        var b = new Border { Width = 2 * r, Height = 2 * r, CornerRadius = new CornerRadius(r), BorderBrush = stroke,
            BorderThickness = new Thickness(2), Background = new SolidColorBrush(Color.Parse("#CCFFFFFF")) };
        Canvas.SetLeft(b, cx - r); Canvas.SetTop(b, cy - r);
        return b;
    }

    private void Append(string path, string kind, string? flag, Point cursor, int count, List<Hint> hints, string headerFormat,
        double namesMs, double extMs, double shownMs, double t1, double t2, double headerMs)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var screen = Screens.ScreenFromVisual(this);
        var all = Screens.All;
        var idx = screen is null ? -1 : all.ToList().IndexOf(screen);
        var winPx = new PixelRect(Position, new PixelSize((int)(Bounds.Width * RenderScaling), (int)(Bounds.Height * RenderScaling)));
        var straddles = screen is null || !screen.Bounds.Contains(winPx.TopLeft) || !screen.Bounds.Contains(winPx.BottomRight);
        var (rl, rt) = DonutGeometry.Place(cursor.X, cursor.Y, RingDiameter, Bounds.Width, Bounds.Height, EdgeMargin);
        var inside = rl >= 0 && rt >= 0 && rl + RingDiameter <= Bounds.Width && rt + RingDiameter <= Bounds.Height;
        var inv = CultureInfo.InvariantCulture;
        string F(double v) => double.IsNaN(v) ? "NaN" : v.ToString("F2", inv);
        var header = "run,seq,tag,flag,renderScaling,screenIdx,screenScaling,screens,windowStraddles,files,formats,headerFormat,namesMs,extHintMs,overlayShownMs,frame1Ms,frame2Ms,headerMs,ringInsideWindow";
        if (!File.Exists(path)) File.WriteAllText(path, header + Environment.NewLine);
        var row = string.Join(",", _runId, _seq, _opt.Tag, flag ?? "ok", RenderScaling.ToString("F2", inv), idx,
            (screen?.Scaling ?? 0).ToString("F2", inv), all.Count, straddles, count,
            string.Join("+", hints.Select(h => h.Format).Distinct()), headerFormat, F(namesMs), F(extMs), F(shownMs), F(t1), F(t2), F(headerMs), inside);
        File.AppendAllText(path, row + Environment.NewLine);
    }
}
