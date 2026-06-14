using System.Windows;

namespace ScanPro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Gecici dosyalari temizle
        TempCleaner.Init();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TempCleaner.Cleanup();
        base.OnExit(e);
    }
}
