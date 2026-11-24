using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppInstaller;

public enum InstallStatus
{
    Pending,
    Downloading,
    Verifying,
    Installing,
    Completed,
    Failed,
    Skipped,
    NeedsUserInput
}

public record InstallResult
{
    public AppEntry App { get; init; } = null!;
    public InstallStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public string? DownloadedPath { get; init; }
    public Exception? Exception { get; init; }

    public bool Succeeded => Status is InstallStatus.Completed;
}

public interface IInstaller
{
    string Name { get; }
    Task<InstallResult> InstallAsync(AppEntry app, string workingDirectory, IProgress<string> progress, CancellationToken ct);
}