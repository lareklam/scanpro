using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ScanPro.src.Core.Services;
using ScanPro.src.OCR.Engines;
using ScanPro.src.AI.Classification;
using ScanPro.src.AI.SmartNaming;
using ScanPro.src.UI.ViewModels;

namespace ScanPro;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var services = new ServiceCollection();
        services.AddSingleton<ImageProcessingService>();
        services.AddSingleton<IOcrEngine, TesseractOcrEngine>();
        services.AddSingleton<IDocumentClassifier, LocalDocumentClassifier>();
        services.AddSingleton<SmartFileNamingService>();
        services.AddTransient<MainViewModel>();
        Services = services.BuildServiceProvider();
        TempFileManager.Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TempFileManager.PurgeAll();
        base.OnExit(e);
    }
}
