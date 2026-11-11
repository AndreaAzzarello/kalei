using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AotWindow;

public partial class App : Application
{
    public override void Initialize()
    {
        StartupMarks.Mark("appInitStart");
        AvaloniaXamlLoader.Load(this);
        StartupMarks.Mark("appInitEnd");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        StartupMarks.Mark("frameworkReady");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
