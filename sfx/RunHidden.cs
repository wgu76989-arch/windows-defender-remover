// RunHidden: launches a process with CREATE_NO_WINDOW, compiled as GUI subsystem (no console).
// Usage: RunHidden.exe <program.exe> [args...]
// PowerRun.exe launches this (GUI app, no console flash), then RunHidden launches the
// real target with CREATE_NO_WINDOW, so the entire chain is window-free.
using System;
using System.Diagnostics;

class RunHidden
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length == 0) return 1;

        string exe = args[0];
        string cmdArgs = "";
        if (args.Length > 1)
            cmdArgs = string.Join(" ", args, 1, args.Length - 1);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = cmdArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                return p.ExitCode;
            }
        }
        catch
        {
            return 1;
        }
    }
}
