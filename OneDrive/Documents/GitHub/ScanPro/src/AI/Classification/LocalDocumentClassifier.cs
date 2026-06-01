using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ScanPro.src.Core.Models;

namespace ScanPro.src.AI.Classification;

public interface IDocumentClassifier
{
    Task<ClassificationResult> ClassifyAsync(string ocrText, CancellationToken ct = default);
}

public class LocalDocumentClassifier : IDocumentClassifier
{
    private static readonly Dictionary<DocumentType, string[]> Keywords = new()
    {
        [DocumentType.Invoice]          = ["fatura","kdv","toplam tutar","invoice","vat","total"],
        [DocumentType.Contract]         = ["sözleşme","madde","imza","contract","agreement","clause"],
        [DocumentType.Receipt]          = ["fiş","makbuz","nakit","receipt","cash","register"],
        [DocumentType.IdentityDocument] = ["kimlik","pasaport","tc kimlik","identity","passport"],
        [DocumentType.BankStatement]    = ["banka","iban","bakiye","bank","balance","statement"],
        [DocumentType.LegalDocument]    = ["mahkeme","dava","karar","court","judgment","law"],
        [DocumentType.ApplicationForm]  = ["başvuru","form","ad soyad","application","form","name"],
    };

    public async Task<ClassificationResult> ClassifyAsync(string ocrText, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(ocrText))
                return new ClassificationResult { Type = DocumentType.Unknown, Confidence = 0f };

            var lower = ocrText.ToLowerInvariant();
            var scores = new Dictionary<DocumentType, float>();

            foreach (var (type, kws) in Keywords)
            {
                float score = kws.Count(kw => lower.Contains(kw));
                scores[type] = score / kws.Length;
            }

            var best = scores.MaxBy(kv => kv.Value);
            return new ClassificationResult
            {
                Type = best.Value > 0.05f ? best.Key : DocumentType.Unknown,
                Confidence = Math.Min(best.Value * 3f, 0.99f),
                AllScores = scores
            };
        }, ct);
    }
}

public class ClassificationResult
{
    public DocumentType Type { get; set; }
    public float Confidence { get; set; }
    public Dictionary<DocumentType, float> AllScores { get; set; } = new();
}
