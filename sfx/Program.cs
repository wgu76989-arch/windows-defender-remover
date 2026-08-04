// DefenderRemover SFX Wrapper
// Extracts embedded payload.zip to temp dir and runs Script_Run.bat
// Compiled with .NET Framework csc.exe (no runtime dependency beyond .NET FX 4.5+)
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;

class DefenderRemoverSFX
{
    [STAThread]
    static void Main()
    {
        string baseDir = Path.Combine(
            Path.GetTempPath(),
            "DefenderRemover_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(baseDir);

        var asm = Assembly.GetExecutingAssembly();
        string payloadName = null;
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (name.EndsWith("payload.zip"))
            {
                payloadName = name;
                break;
            }
        }

        if (payloadName != null)
        {
            using (var stream = asm.GetManifestResourceStream(payloadName))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string outPath = Path.Combine(baseDir, entry.FullName.Replace('/', '\\'));
                    string dir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    using (var es = entry.Open())
                    using (var fs = File.Create(outPath))
                    {
                        es.CopyTo(fs);
                    }
                }
            }
        }

        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(baseDir, "Script_Run.bat"),
            WorkingDirectory = baseDir,
            UseShellExecute = false
        });
        proc.WaitForExit();
    }
}