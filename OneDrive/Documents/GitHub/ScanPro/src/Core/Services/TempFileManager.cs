using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScanPro.src.Core.Services;

public static class TempFileManager
{
    private static readonly string TempDir =
        Path.Combine(Path.GetTempPath(), $"ScanPro_{System.Environment.ProcessId}");
    private static readonly HashSet<string> Tracked = new();
    private static readonly object Lock = new();

    public static void Initialize()
    {
        if (Directory.Exists(TempDir)) PurgeAll();
        Directory.CreateDirectory(TempDir);
    }

    public static string CreateTempPath(string ext = ".tmp")
    {
        var path = Path.Combine(TempDir, $"{System.Guid.NewGuid():N}{ext}");
        lock (Lock) Tracked.Add(path);
        return path;
    }

    public static void SecureDelete(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var size = new FileInfo(path).Length;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Write))
            {
                var zeros = new byte[Math.Min(size, 65536)];
                long written = 0;
                while (written < size)
                {
                    int chunk = (int)Math.Min(zeros.Length, size - written);
                    fs.Write(zeros, 0, chunk);
                    written += chunk;
                }
                fs.Flush(true);
            }
            File.Delete(path);
        }
        catch { }
        lock (Lock) Tracked.Remove(path);
    }

    public static void PurgeAll()
    {
        lock (Lock)
        {
            foreach (var f in Tracked.ToList()) SecureDelete(f);
        }
        try { if (Directory.Exists(TempDir)) Directory.Delete(TempDir, true); } catch { }
    }

    public static string TempDirectory => TempDir;
}
