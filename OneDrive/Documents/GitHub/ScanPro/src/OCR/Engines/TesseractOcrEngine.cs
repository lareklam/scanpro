using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ScanPro.src.Core.Models;
using ScanPro.src.Core.Services;
using SixLabors.ImageSharp;
using Tesseract;

namespace ScanPro.src.OCR.Engines;

public interface IOcrEngine
{
    void Initialize();
    Task<OcrResult> RecognizeAsync(Image image, string[] languages, CancellationToken ct = default);
    bool IsInitialized { get; }
}

public class TesseractOcrEngine : IOcrEngine, IDisposable
{
    private readonly Dictionary<string, TesseractEngine> _engines = new();
    private readonly string _tessDataPath;
    private bool _initialized;

    public bool IsInitialized => _initialized;

    public TesseractOcrEngine()
    {
        _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
    }

    public void Initialize()
    {
        if (!Directory.Exists(_tessDataPath))
        {
            // tessdata not found — skip gracefully, OCR will be unavailable
            return;
        }
        TryLoadEngine("tur");
        TryLoadEngine("eng");
        _initialized = true;
    }

    private TesseractEngine? TryLoadEngine(string lang)
    {
        if (_engines.TryGetValue(lang, out var existing)) return existing;
        var file = Path.Combine(_tessDataPath, $"{lang}.traineddata");
        if (!File.Exists(file)) return null;
        var engine = new TesseractEngine(_tessDataPath, lang, EngineMode.Default);
        _engines[lang] = engine;
        return engine;
    }

    public async Task<OcrResult> RecognizeAsync(Image image, string[] languages, CancellationToken ct = default)
    {
        if (!_initialized)
            return new OcrResult { Text = "", Confidence = 0f, Languages = languages };

        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            var tempPath = TempFileManager.CreateTempPath(".png");
            try
            {
                image.SaveAsPng(tempPath);
                return RecognizeFile(tempPath, languages);
            }
            finally
            {
                TempFileManager.SecureDelete(tempPath);
            }
        }, ct);
    }

    private OcrResult RecognizeFile(string imagePath, string[] languages)
    {
        var langStr = string.Join("+", languages.Select(l => l.ToLower()));
        TesseractEngine? engine = null;

        if (languages.Length == 1)
            engine = TryLoadEngine(languages[0]);
        else
        {
            if (!_engines.TryGetValue(langStr, out engine))
            {
                try
                {
                    engine = new TesseractEngine(_tessDataPath, langStr, EngineMode.Default);
                    _engines[langStr] = engine;
                }
                catch
                {
                    engine = TryLoadEngine(languages[0]);
                }
            }
        }

        if (engine == null)
            return new OcrResult { Text = "", Confidence = 0f, Languages = languages };

        using var pix = Pix.LoadFromFile(imagePath);
        using var page = engine.Process(pix);

        return new OcrResult
        {
            Text = page.GetText()?.Trim() ?? "",
            Confidence = page.GetMeanConfidence(),
            Languages = languages,
            Words = new List<OcrWord>()
        };
    }

    public void Dispose()
    {
        foreach (var e in _engines.Values) e.Dispose();
        _engines.Clear();
    }
}

public class OcrResult
{
    public string Text { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public List<OcrWord> Words { get; set; } = new();
    public string[] Languages { get; set; } = Array.Empty<string>();
}
