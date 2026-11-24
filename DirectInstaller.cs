using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AppInstaller;

public sealed class DirectInstaller : IInstaller
{
    public string Name => "Direct download (publisher's official site)";

    public async Task<InstallResult> InstallAsync(AppEntry app, string workingDirectory, IProgress<string> progress, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string? installerPath = null;

        try
        {
            if (string.IsNullOrWhiteSpace(app.OfficialUrl))
            {
                return Failed(app, sw, "No official download URL configured.", null);
            }

            progress.Report($"Downloading {app.Name} from {app.OfficialUrl}...");
            installerPath = Path.Combine(workingDirectory, app.InstallerFileName);

            await DownloadFileAsync(app.OfficialUrl, installerPath, progress, ct);

            progress.Report($"Verifying digital signature of {app.InstallerFileName}...");
            if (!VerifySignature(installerPath, out var sigDetail))
            {
                return Failed(app, sw, $"Signature verification failed: {sigDetail}", null);
            }

            progress.Report($"Installing {app.Name}...");
            var installResult = RunInstaller(app, installerPath, ct);
            sw.Stop();
            return installResult with { Duration = sw.Elapsed, DownloadedPath = installerPath };
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return Failed(app, sw, "Cancelled.", null) with { DownloadedPath = installerPath };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Failed(app, sw, ex.Message, ex) with { DownloadedPath = installerPath };
        }
    }

    private static async Task DownloadFileAsync(string url, string destPath, IProgress<string> progress, CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0L;
        using var sourceStream = await response.Content.ReadAsStreamAsync(ct);
        using var destStream = File.Create(destPath);

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;
        while ((read = await sourceStream.ReadAsync(buffer, ct)) > 0)
        {
            await destStream.WriteAsync(buffer.AsMemory(0, read), ct);
            totalRead += read;
            if (totalBytes > 0 && totalRead % (1024 * 1024) < read)
            {
                progress.Report($"Downloading... {totalRead / 1024 / 1024} MB of {totalBytes / 1024 / 1024} MB");
            }
        }
    }

    private static InstallResult RunInstaller(AppEntry app, string installerPath, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = app.SilentArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(installerPath) ?? string.Empty
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            return new InstallResult
            {
                App = app,
                Status = InstallStatus.Failed,
                Message = "Unable to start installer.",
                Duration = TimeSpan.Zero
            };
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        var exited = process.WaitForExit((int)TimeSpan.FromMinutes(20).TotalMilliseconds);
        if (!exited)
        {
            try { process.Kill(); } catch { }
            return new InstallResult
            {
                App = app,
                Status = InstallStatus.Failed,
                Message = "Installer timed out after 20 minutes.",
                Duration = TimeSpan.FromMinutes(20)
            };
        }

        Task.WaitAll(outputTask, errorTask);
        var output = outputTask.Result;
        var error = errorTask.Result;

        if (process.ExitCode == 0 || process.ExitCode == 3010)
        {
            return new InstallResult
            {
                App = app,
                Status = InstallStatus.Completed,
                Message = process.ExitCode == 3010
                    ? "Installed successfully (restart required to finish)."
                    : "Installed successfully.",
                Duration = TimeSpan.Zero
            };
        }

        var detail = string.IsNullOrWhiteSpace(error) ? output : error;
        return new InstallResult
        {
            App = app,
            Status = InstallStatus.Failed,
            Message = $"Installer exited with code {process.ExitCode}: {detail.Trim().TakeLast(300)}",
            Duration = TimeSpan.Zero
        };
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

    #region Signature verification (WinVerifyTrust)

    [StructLayout(LayoutKind.Sequential)]
    private struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        public IntPtr pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [DllImport("wintrust.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int WinVerifyTrust(IntPtr hwnd, IntPtr pgActionID, IntPtr pWinTrustData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcessHeap();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, uint dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

    private const uint WTD_CHOICE_FILE = 1;
    private const uint WTD_UICONTEXT_EXECUTE = 0;
    private const uint WTD_REVOKE_NONE = 0;
    private const uint WTD_USE_DEFAULT_OSVER_CHECK = 0;
    private const uint WTD_SAFER_FLAG = 0;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WINTRUST_DATA
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
    }

    private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");

    private static bool VerifySignature(string filePath, out string detail)
    {
        detail = string.Empty;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            detail = "Not running on Windows; skipping signature check.";
            return true;
        }

        IntPtr hHeap = GetProcessHeap();
        IntPtr pFileInfo = HeapAlloc(hHeap, 0, (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>());
        IntPtr pWinTrustData = HeapAlloc(hHeap, 0, (uint)Marshal.SizeOf<WINTRUST_DATA>());
        if (pFileInfo == IntPtr.Zero || pWinTrustData == IntPtr.Zero)
        {
            detail = "Failed to allocate memory for verification.";
            HeapFree(hHeap, 0, pFileInfo);
            HeapFree(hHeap, 0, pWinTrustData);
            return false;
        }

        try
        {
            var fileInfo = new WINTRUST_FILE_INFO
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                pcwszFilePath = Marshal.StringToCoTaskMemUni(filePath)
            };
            Marshal.StructureToPtr(fileInfo, pFileInfo, false);

            var wvtData = new WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = WTD_UICONTEXT_EXECUTE,
                fdwRevocationChecks = WTD_REVOKE_NONE,
                dwUnionChoice = WTD_CHOICE_FILE,
                pFile = pFileInfo,
                dwStateAction = WTD_REVOKE_NONE,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = IntPtr.Zero,
                dwProvFlags = WTD_SAFER_FLAG,
                dwUIContext = 0
            };
            Marshal.StructureToPtr(wvtData, pWinTrustData, false);

            IntPtr actionPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf<Guid>());
            Marshal.StructureToPtr(WINTRUST_ACTION_GENERIC_VERIFY_V2, actionPtr, false);

            int result = WinVerifyTrust(IntPtr.Zero, actionPtr, pWinTrustData);

            Marshal.FreeCoTaskMem(actionPtr);

            bool success = result == 0;
            detail = success
                ? "Valid digital signature."
                : $"Signature verification failed (WinVerifyTrust error 0x{result:X}).";
            return success;
        }
        catch (Exception ex)
        {
            detail = $"Verification exception: {ex.Message}";
            return false;
        }
        finally
        {
            HeapFree(hHeap, 0, pFileInfo);
            HeapFree(hHeap, 0, pWinTrustData);
        }
    }

    #endregion
}