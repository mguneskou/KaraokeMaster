using System.Diagnostics;

namespace KaraokeMaster.Core.Separation;

/// <summary>
/// Finds a system Python interpreter to bootstrap the venv from, via Windows's "py" launcher.
/// Prefers well-established versions PyTorch is known to support over whatever the newest
/// installed version happens to be, since PyTorch wheels typically lag new Python releases.
/// </summary>
public static class PythonLocator
{
    private static readonly string[] PreferredVersions = ["3.12", "3.11", "3.10", "3.13"];

    public static string? FindSystemPython()
    {
        foreach (var version in PreferredVersions)
        {
            var path = TryResolve("py", $"-{version} -c \"import sys; print(sys.executable)\"");
            if (path is not null)
            {
                return path;
            }
        }

        return TryResolve("py", "-3 -c \"import sys; print(sys.executable)\"")
            ?? TryResolve("python", "-c \"import sys; print(sys.executable)\"")
            ?? TryResolve("python3", "-c \"import sys; print(sys.executable)\"");
    }

    private static string? TryResolve(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            return process.ExitCode == 0 && File.Exists(output) ? output : null;
        }
        catch
        {
            return null;
        }
    }
}
