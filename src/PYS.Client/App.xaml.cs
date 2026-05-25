using System.Reflection;

namespace PYS.Client;

public partial class App : Application
{
    private static readonly string CrashLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PYS.Client",
        "crash.log");

    public App()
    {
        InitializeComponent();

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            WriteCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            WriteCrash("UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        Microsoft.Maui.Controls.Application.Current!.PropertyChanged += (_, _) => { };
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell());

    public static void LogException(string source, Exception? ex) => WriteCrash(source, ex);

    private static void WriteCrash(string source, Exception? ex)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);
            File.AppendAllText(CrashLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}\n{ex}\n---\n");
        }
        catch { /* ignore — last-resort logger */ }
    }
}
