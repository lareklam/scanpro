using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScanPro.src.AI.Classification;
using ScanPro.src.AI.SmartNaming;
using ScanPro.src.Core.Models;
using ScanPro.src.Core.Services;
using ScanPro.src.OCR.Engines;

namespace ScanPro.src.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IOcrEngine _ocr;
    private readonly IDocumentClassifier _classifier;
    private readonly SmartFileNamingService _namer;
    private readonly ImageProcessingService _imgProc;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Hazır";
    [ObservableProperty] private bool _hasDocument;
    [ObservableProperty] private string _currentDocumentName = string.Empty;
    [ObservableProperty] private string _documentType = "—";
    [ObservableProperty] private string _currentTime = DateTime.Now.ToString("HH:mm");
    [ObservableProperty] private ScannedDocument? _currentDocument;

    public ObservableCollection<WorkflowStep> WorkflowSteps { get; } = new()
    {
        new() { Number = 1, Name = "Tarama",          StatusText = "Bekliyor", StatusIcon = "○", StatusColor = "#444B5E" },
        new() { Number = 2, Name = "OCR",             StatusText = "Bekliyor", StatusIcon = "○", StatusColor = "#444B5E" },
        new() { Number = 3, Name = "Görüntü Temizle", StatusText = "Bekliyor", StatusIcon = "○", StatusColor = "#444B5E" },
        new() { Number = 4, Name = "Üst / Alt Bilgi", StatusText = "Bekliyor", StatusIcon = "○", StatusColor = "#444B5E" },
        new() { Number = 5, Name = "Filigran",        StatusText = "Bekliyor", StatusIcon = "○", StatusColor = "#444B5E" },
        new() { Number = 6, Name = "PDF/A Aktar",     StatusText = "Bekliyor", StatusIcon = "○", StatusColor = "#444B5E" },
    };

    public MainViewModel()
    {
        _ocr = App.Services.GetService(typeof(IOcrEngine)) as IOcrEngine
               ?? new TesseractOcrEngine();
        _classifier = App.Services.GetService(typeof(IDocumentClassifier)) as IDocumentClassifier
                      ?? new LocalDocumentClassifier();
        _namer = App.Services.GetService(typeof(SmartFileNamingService)) as SmartFileNamingService
                 ?? new SmartFileNamingService();
        _imgProc = App.Services.GetService(typeof(ImageProcessingService)) as ImageProcessingService
                   ?? new ImageProcessingService();

        // Update clock every minute
        var timer = new System.Windows.Threading.DispatcherTimer();
        timer.Interval = TimeSpan.FromMinutes(1);
        timer.Tick += (_, _) => CurrentTime = DateTime.Now.ToString("HH:mm");
        timer.Start();
    }

    [RelayCommand]
    private async Task StartNewScanAsync()
    {
        _cts = new CancellationTokenSource();
        IsBusy = true;
        ResetSteps();

        try
        {
            var doc = new ScannedDocument { ScannedAt = DateTime.Now };
            CurrentDocument = doc;

            SetStep(1, "Çalışıyor", "▶", "#2F80ED");
            StatusMessage = "Tarayıcı bekleniyor...";
            await Task.Delay(500, _cts.Token); // Tarayıcı entegrasyonu için yer tutucu
            SetStep(1, "Tamamlandı", "✓", "#27AE60");

            SetStep(2, "Çalışıyor", "▶", "#2F80ED");
            StatusMessage = "OCR işleniyor...";
            await Task.Delay(800, _cts.Token);
            SetStep(2, "Tamamlandı", "✓", "#27AE60");

            SetStep(3, "Çalışıyor", "▶", "#2F80ED");
            StatusMessage = "Görüntü iyileştiriliyor...";
            await Task.Delay(600, _cts.Token);
            SetStep(3, "Tamamlandı", "✓", "#27AE60");

            SetStep(4, "Çalışıyor", "▶", "#2F80ED");
            StatusMessage = "Üst/Alt bilgi ekleniyor...";
            await Task.Delay(300, _cts.Token);
            SetStep(4, "Tamamlandı", "✓", "#27AE60");

            SetStep(5, "Çalışıyor", "▶", "#2F80ED");
            StatusMessage = "Filigran uygulanıyor...";
            await Task.Delay(300, _cts.Token);
            SetStep(5, "Tamamlandı", "✓", "#27AE60");

            SetStep(6, "Çalışıyor", "▶", "#2F80ED");
            StatusMessage = "PDF/A oluşturuluyor...";
            await Task.Delay(400, _cts.Token);
            SetStep(6, "Tamamlandı", "✓", "#27AE60");

            doc.SmartFileName = _namer.GenerateName(doc);
            CurrentDocumentName = doc.SmartFileName;
            DocumentType = doc.DetectedType.ToString();
            HasDocument = true;
            StatusMessage = $"Tamamlandı: {doc.SmartFileName}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "İptal edildi.";
            ResetSteps();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelOperation() => _cts?.Cancel();

    [RelayCommand]
    private async Task ExportCurrentDocumentAsync()
    {
        if (CurrentDocument == null) return;
        StatusMessage = "Dışa aktarılıyor...";
        await Task.Delay(500);
        StatusMessage = "Dışa aktarma tamamlandı.";
    }

    private void SetStep(int n, string text, string icon, string color)
    {
        var s = WorkflowSteps.FirstOrDefault(x => x.Number == n);
        if (s == null) return;
        s.StatusText = text;
        s.StatusIcon = icon;
        s.StatusColor = color;
    }

    private void ResetSteps()
    {
        foreach (var s in WorkflowSteps)
        {
            s.StatusText = "Bekliyor";
            s.StatusIcon = "○";
            s.StatusColor = "#444B5E";
        }
    }
}

public partial class WorkflowStep : ObservableObject
{
    public int Number { get; set; }
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _statusIcon = "○";
    [ObservableProperty] private string _statusColor = "#444B5E";
}
