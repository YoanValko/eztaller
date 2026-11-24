using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AppInstaller;

public sealed class RebootManager : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private Task? _countdownTask;
    private bool _rebootScheduled;
    private readonly object _lock = new();

    public event Action<int>? CountdownTick;
    public event Action? RebootCancelled;
    public event Action? RebootInitiated;

    public bool IsCountingDown => _countdownTask != null && !_countdownTask.IsCompleted;

    public void StartCountdown(int seconds, Action onReboot)
    {
        lock (_lock)
        {
            if (_rebootScheduled) return;
            _rebootScheduled = true;
            _cts.Token.ThrowIfCancellationRequested();
        }

        _countdownTask = Task.Run(async () =>
        {
            try
            {
                for (int i = seconds; i > 0; i--)
                {
                    if (_cts.Token.IsCancellationRequested)
                    {
                        RebootCancelled?.Invoke();
                        return;
                    }
                    CountdownTick?.Invoke(i);
                    await Task.Delay(1000, _cts.Token);
                }

                if (_cts.Token.IsCancellationRequested)
                {
                    RebootCancelled?.Invoke();
                    return;
                }

                RebootInitiated?.Invoke();
                RebootNow();
                onReboot();
            }
            catch (OperationCanceledException)
            {
                RebootCancelled?.Invoke();
            }
        }, _cts.Token);
    }

    public void Cancel()
    {
        _cts.Cancel();
        lock (_lock) { _rebootScheduled = false; }
    }

    public void RebootNow()
    {
        var reason = NativeMethods.SHTDN_REASON_FLAG_PLANNED |
                     NativeMethods.SHTDN_REASON_MAJOR_OTHER |
                     NativeMethods.SHTDN_REASON_MINOR_OTHER;

        bool ok = NativeMethods.InitiateSystemShutdownExW(
            null,
            "Winget Installer: Finished installing applications. Restarting now.",
            1,
            true,
            true,
            reason);

        if (!ok)
        {
            int err = Marshal.GetLastWin32Error();
            NativeMethods.ExitWindowsEx(
                NativeMethods.EWX_REBOOT | NativeMethods.EWX_FORCEIFHUNG,
                reason);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}