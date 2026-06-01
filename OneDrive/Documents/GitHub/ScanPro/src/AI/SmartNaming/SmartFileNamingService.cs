using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ScanPro.src.Core.Models;

namespace ScanPro.src.AI.SmartNaming;

public class SmartFileNamingService
{
    private static readonly Dictionary<DocumentType, string> TypeNames = new()
    {
        [DocumentType.Invoice]          = "Fatura",
        [DocumentType.Contract]         = "Sozlesme",
        [DocumentType.Receipt]          = "Makbuz",
        [DocumentType.IdentityDocument] = "Kimlik",
        [DocumentType.BankStatement]    = "BankaEkstresi",
        [DocumentType.LegalDocument]    = "HukukiBelge",
        [DocumentType.ApplicationForm]  = "BasvuruFormu",
        [DocumentType.Unknown]          = "Belge",
    };

    public string GenerateName(ScannedDocument doc, string extension = ".pdf")
    {
        string date = doc.ScannedAt.ToString("yyyy-MM-dd");
        string type = TypeNames.GetValueOrDefault(doc.DetectedType, "Belge");
        string reference = ExtractReference(doc);
        return Sanitize($"{date}_{type}_{reference}") + extension;
    }

    private static string ExtractReference(ScannedDocument doc)
    {
        var allText = string.Join(" ", doc.Pages
            .Where(p => p.OcrText != null).Select(p => p.OcrText));

        if (string.IsNullOrWhiteSpace(allText))
            return doc.ScannedAt.ToString("HHmm");

        var m = Regex.Match(allText, @"(?:fatura|inv|ref|no)[:\s#]*([A-Z0-9\-/]{4,20})",
            RegexOptions.IgnoreCase);
        if (m.Success) return Sanitize(m.Groups[1].Value);

        return doc.ScannedAt.ToString("HHmm");
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var result = new string(name.Replace(' ', '_')
            .Where(c => !invalid.Contains(c)).ToArray());
        return result.Length > 60 ? result[..60] : result;
    }
}
