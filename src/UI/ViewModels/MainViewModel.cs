using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ScanPro.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _statusMessage = "Hazir";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _selectedScanner;
    [ObservableProperty] private string _selectedFormat = "PDF/A";
    [ObservableProperty] private int _selectedDpi = 300;
    [ObservableProperty] private string _selectedColorMode = "Gri Ton";
    [ObservableProperty] private string _scannerInfo = "Tarayicilar aranıyor...";
    [ObservableProperty] private string _headerLeft = "Kurum Adi";
    [ObservableProperty] private string _headerCenter = "";
    [ObservableProperty] private string _headerRight = "Tarih";
    [ObservableProperty] private string _footerLeft = "Gizli";
    [ObservableProperty] private string _footerRight = "Sayfa N/T";
    [ObservableProperty] private string _watermarkText = "GIZLI";
    [ObservableProperty] private double _watermarkSize = 52;
    [ObservableProperty] private double _watermarkOpacity = 0.12;
    [ObservableProperty] private double _watermarkAngle = -35;
    [ObservableProperty] private bool _hasDocument;
    [ObservableProperty] private string _currentTime = DateTime.Now.ToString("HH:mm");

    public ObservableCollection<string> Scanners { get; } = new();
    public ObservableCollection<string> Formats { get; } = new() { "PDF", "PDF/A", "TIFF", "JPEG", "PNG" };
    public ObservableCollection<int> DpiList { get; } = new() { 75, 100, 150, 200, 300, 400, 600 };
    public ObservableCollection<string> ColorModes { get; } = new() { "Renkli", "Gri Ton", "Siyah Beyaz" };
    public ObservableCollection<string> SystemFonts { get; } = new();
    [ObservableProperty] private string _selectedFont = "Segoe UI";

    public ObservableCollection<StepItem> Steps { get; } = new()
    {
        new(1,"Tarama"), new(2,"OCR"), new(3,"Temizle"),
        new(4,"Ust/Alt"), new(5,"Filigran"), new(6,"Kaydet"),
    };

    public MainViewModel()
    {
        foreach (var f in Fonts.SystemFontFamilies.OrderBy(x => x.Source))
            SystemFonts.Add(f.Source);

        var t = new System.Windows.Threading.DispatcherTimer
            { Interval = TimeSpan.FromSeconds(30) };
        t.Tick += (_, _) => CurrentTime = DateTime.Now.ToString("HH:mm");
        t.Start();

        _ = FindScannersAsync();
    }

    [RelayCommand]
    private async Task FindScannersAsync()
    {
        ScannerInfo = "Tarayicilar aranıyor...";
        Scanners.Clear();

        await Task.Run(() =>
        {
            // WIA ile USB/ag tarayicilar
            try
            {
                var t = Type.GetTypeFromProgID("WIA.DeviceManager");
                if (t != null)
                {
                    dynamic mgr = Activator.CreateInstance(t)!;
                    foreach (dynamic d in mgr.DeviceInfos)
                    {
                        try
                        {
                            if ((int)d.Type != 1) continue;
                            string n = (string)d.Properties["Name"].Value;
                            System.Windows.Application.Current.Dispatcher.Invoke(()
                                => Scanners.Add("[USB] " + n));
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // Ag tarama - sadece eSCL destekli cihazlar
            try
            {
                string? sub = GetSubnet();
                if (sub != null)
                {
                    Parallel.For(1, 255, new ParallelOptions { MaxDegreeOfParallelism = 50 }, i =>
                    {
                        string ip = sub + "." + i;
                        try
                        {
                            using var http = new System.Net.Http.HttpClient
                                { Timeout = TimeSpan.FromMilliseconds(800) };
                            var r = http.GetAsync("http://" + ip + "/eSCL/ScannerCapabilities").Result;
                            if (r.IsSuccessStatusCode)
                                System.Windows.Application.Current.Dispatcher.Invoke(()
                                    => Scanners.Add("[AG] eSCL - " + ip));
                        }
                        catch { }
                    });
                }
            }
            catch { }
        });

        if (Scanners.Count > 0)
        {
            SelectedScanner = Scanners[0];
            ScannerInfo = Scanners.Count + " tarayici bulundu";
        }
        else
        {
            Scanners.Add("Tarayici bulunamadi");
            SelectedScanner = Scanners[0];
            ScannerInfo = "Tarayici bulunamadi - USB veya ag baglantisinizi kontrol edin";
        }
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (SelectedScanner == null || SelectedScanner == "Tarayici bulunamadi")
        {
            StatusMessage = "Lutfen once bir tarayici secin!";
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        HasDocument = false;
        foreach (var s in Steps) s.Reset();

        try
        {
            await DoStep(1, "Taranıyor: " + SelectedScanner, 1500);
            await DoStep(2, "OCR isleniyor...", 1000);
            await DoStep(3, "Goruntu iyilestiriliyor...", 700);
            await DoStep(4, "Ust/Alt bilgi ekleniyor...", 400);
            await DoStep(5, "Filigran uygulanıyor...", 400);
            await DoStep(6, SelectedFormat + " kaydediliyor...", 600);

            HasDocument = true;
            StatusMessage = "Tamamlandi!";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Iptal edildi.";
            foreach (var s in Steps) s.Reset();
        }
        catch (Exception ex)
        {
            StatusMessage = "Hata: " + ex.Message;
        }
        finally { IsBusy = false; }
    }

    private async Task DoStep(int n, string msg, int ms)
    {
        _cts!.Token.ThrowIfCancellationRequested();
        StatusMessage = msg;
        Steps.First(x => x.Number == n).SetActive();
        await Task.Delay(ms, _cts.Token);
        Steps.First(x => x.Number == n).SetDone();
    }

    [RelayCommand] private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private async Task ExportAsync()
    {
        StatusMessage = "Disa aktarılıyor...";
        await Task.Delay(600);
        StatusMessage = "Disa aktarma tamamlandi.";
    }

    private static string? GetSubnet()
    {
        foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
                var p = ua.Address.ToString().Split('.');
                if (p.Length == 4 && p[0] != "127") return p[0] + "." + p[1] + "." + p[2];
            }
        }
        return null;
    }
}

public partial class StepItem : ObservableObject
{
    public int Number { get; }
    public string Name { get; }
    [ObservableProperty] private string _icon = "○";
    [ObservableProperty] private string _status = "Bekliyor";
    [ObservableProperty] private string _color = "#555";

    public StepItem(int n, string name) { Number = n; Name = name; }
    public void SetActive() { Icon = "▶"; Status = "Calisıyor"; Color = "#2F80ED"; }
    public void SetDone()   { Icon = "✓"; Status = "Tamam";     Color = "#27AE60"; }
    public void Reset()     { Icon = "○"; Status = "Bekliyor";  Color = "#555"; }
}
