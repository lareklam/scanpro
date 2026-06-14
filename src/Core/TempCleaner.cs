using System;
using System.IO;

namespace ScanPro;

public static class TempCleaner
{
    private static readonly string TempDir =
        Path.Combine(Path.GetTempPath(), "ScanPro_" + Environment.ProcessId);

    public static void Init()
    {
        if (Directory.Exists(TempDir)) Cleanup();
        Directory.CreateDirectory(TempDir);
    }

    public static string GetTempFile(string ext = ".tmp")
        => Path.Combine(TempDir, Guid.NewGuid().ToString("N") + ext);

    public static void Cleanup()
    {
        try { if (Directory.Exists(TempDir)) Directory.Delete(TempDir, true); }
        catch { }
    }
}
