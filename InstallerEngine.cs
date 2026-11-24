using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AppInstaller;

public sealed class InstallerEngine
{
    private readonly WingetInstaller _winget = new();
    private readonly DirectInstaller _direct = new();

    public async Task<List<InstallResult>> InstallAllAsync(
        List<AppEntry> apps,
        string workingDirectory,
        IProgress<string> progress,
        CancellationToken ct)
    {
        var results = new List<InstallResult>();

        foreach (var app in apps)
        {
            ct.ThrowIfCancellationRequested();

            InstallResult result;
            if (!string.IsNullOrWhiteSpace(app.WingetId) && WingetAvailable())
            {
                result = await _winget.InstallAsync(app, workingDirectory, progress, ct);
                if (result.Status == InstallStatus.Failed && result.Message.Contains("interaction", StringComparison.OrdinalIgnoreCase))
                {
                    progress.Report($"winget reported interaction needed for {app.Name}; trying direct download...");
                    result = await _direct.InstallAsync(app, workingDirectory, progress, ct);
                }
            }
            else
            {
                result = await _direct.InstallAsync(app, workingDirectory, progress, ct);
            }

            results.Add(result);
        }

        return results;
    }

    private static bool WingetAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            p.WaitForExit(2000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}