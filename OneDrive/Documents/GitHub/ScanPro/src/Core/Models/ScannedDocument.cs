using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;

namespace ScanPro.src.Core.Models;

public class ScannedDocument
{
    public Guid Id { get; } = Guid.NewGuid();
    public string SessionName { get; set; } = string.Empty;
    public List<DocumentPage> Pages { get; set; } = new();
    public DocumentType DetectedType { get; set; } = DocumentType.Unknown;
    public float ClassificationConfidence { get; set; }
    public string SmartFileName { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; } = DateTime.Now;
    public ScanSettings ScanSettings { get; set; } = new();
    public bool IsOcrComplete { get; set; }
    public string PrimaryLanguage { get; set; } = "tur";

    public void DisposeImages()
    {
        foreach (var p in Pages) p.Dispose();
    }
}

public class DocumentPage : IDisposable
{
    public int PageNumber { get; set; }
    public Image? OriginalImage { get; set; }
    public Image? ProcessedImage { get; set; }
    public string? OcrText { get; set; }
    public float OcrConfidence { get; set; }
    public List<OcrWord> OcrWords { get; set; } = new();
    public bool IsBlank { get; set; }
    public int RotationDegrees { get; set; }
    public string? TempFilePath { get; set; }

    public void Dispose()
    {
        OriginalImage?.Dispose();
        ProcessedImage?.Dispose();
        OcrText = null;
        OcrWords.Clear();
        if (TempFilePath != null)
            ScanPro.src.Core.Services.TempFileManager.SecureDelete(TempFilePath);
    }
}

public class OcrWord
{
    public string Text { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public System.Drawing.RectangleF Bounds { get; set; }
}

public class ScanSettings
{
    public int Dpi { get; set; } = 300;
    public ColorMode ColorMode { get; set; } = ColorMode.Grayscale;
    public bool AutoCrop { get; set; } = true;
    public bool AutoDeskew { get; set; } = true;
    public bool RemoveBlankPages { get; set; } = true;
    public bool ShadowRemoval { get; set; } = false;
    public int Brightness { get; set; } = 50;
    public int Contrast { get; set; } = 65;
    public int Sharpness { get; set; } = 40;
    public bool Duplex { get; set; } = false;
}

public enum DocumentType
{
    Unknown, Invoice, Contract, Receipt,
    IdentityDocument, BankStatement, LegalDocument, ApplicationForm
}

public enum ColorMode { Color, Grayscale, BlackWhite }
