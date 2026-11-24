using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppInstaller;

public sealed class WingetInstaller : IInstaller
{
    public string Name => "Windows Package Manager (winget)";

    public async Task<InstallResult> InstallAsync(AppEntry app, string workingDirectory, IProgress<string> progress, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            progress.Report($"Installing {app.Name} via winget...");

            var args = new List<string>
            {
                "install",
                "--id", app.WingetId,
                "--accept-package-agreements",
                "--accept-source-agreements",
                "--silent",
                "--location", workingDirectory
            };

            var psi = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return Failed(app, sw, "Unable to start winget.", null);
            }

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0)
            {
                sw.Stop();
                return new InstallResult
                {
                    App = app,
                    Status = InstallStatus.Completed,
                    Message = "Installed successfully.",
                    Duration = sw.Elapsed
                };
            }

            var detail = string.IsNullOrWhiteSpace(error) ? output : error;
            if (detail.Contains("already installed", StringComparison.OrdinalIgnoreCase))
            {
                return new InstallResult
                {
                    App = app,
                    Status = InstallStatus.Completed,
                    Message = "Already installed.",
                    Duration = sw.Elapsed
                };
            }

            return Failed(app, sw, $"winget failed (exit {process.ExitCode}): {detail.Trim().TakeLast(300)}", null);
        }
        catch (OperationCanceledException)
        {
            return Failed(app, sw, "Cancelled.", null);
        }
        catch (Exception ex)
        {
            return Failed(app, sw, ex.Message, ex);
        }
    }

    private static InstallResult Failed(AppEntry app, Stopwatch sw, string message, Exception? ex)
    {
        sw.Stop();
        return new InstallResult
        {
            App = app,
            Status = InstallStatus.Failed,
            Message = message,
            Duration = sw.Elapsed,
            Exception = ex
        };
    }
}