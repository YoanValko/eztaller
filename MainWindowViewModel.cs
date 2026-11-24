using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace AppInstaller;

public sealed class AppViewModel : INotifyPropertyChanged
{
    public AppEntry Entry { get; }
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSelected)); }
    }
    public bool IsInstalling
    {
        get => _isInstalling;
        set { _isInstalling = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); OnPropertyChanged(nameof(StatusIcon)); }
    }
    public InstallStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(StatusIcon)); }
    }
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public Brush StatusColor => _status switch
    {
        InstallStatus.Pending or InstallStatus.Downloading or InstallStatus.Verifying or InstallStatus.Installing => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF007ACC")),
        InstallStatus.Completed => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF107C10")),
        InstallStatus.Failed => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC42B1C")),
        InstallStatus.Skipped => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080")),
        InstallStatus.NeedsUserInput => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFA500")),
        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF666666"))
    };

    public string StatusIcon => _status switch
    {
        InstallStatus.Completed => "\u2713",
        InstallStatus.Failed => "\u2717",
        InstallStatus.NeedsUserInput => "\u26A0",
        _ => string.Empty
    };

    private bool _isSelected;
    private bool _isInstalling;
    private InstallStatus _status = InstallStatus.Pending;
    private string _statusText = "Waiting";

    public AppViewModel(AppEntry entry)
    {
        Entry = entry;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    public ObservableCollection<AppViewModel> Apps { get; } = new();
    public ObservableCollection<string> Log { get; } = new();

    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanInstall)); OnPropertyChanged(nameof(InstallButtonText)); InstallCommand?.RaiseCanExecuteChanged(); }
    }
    public bool CanInstall => !IsBusy && Apps.Any(a => a.IsSelected);
    public string InstallButtonText => IsBusy ? "Installing..." : "Install selected";
    public string StatusSummary
    {
        get => _statusSummary;
        private set { _statusSummary = value; OnPropertyChanged(); }
    }
    public int SelectedCount => Apps.Count(a => a.IsSelected);
    public bool AllSelected
    {
        get => Apps.Count > 0 && Apps.All(a => a.IsSelected);
        set
        {
            if (value)
            {
                foreach (var a in Apps) a.IsSelected = true;
            }
            OnPropertyChanged();
            RefreshSelectionState();
        }
    }
    public bool NoneSelected
    {
        get => Apps.Count > 0 && !Apps.Any(a => a.IsSelected);
        set
        {
            if (value)
            {
                foreach (var a in Apps) a.IsSelected = false;
            }
            OnPropertyChanged();
            RefreshSelectionState();
        }
    }
    public double OverallProgress
    {
        get => _overallProgress;
        private set { _overallProgress = value; OnPropertyChanged(); }
    }
    public string ProgressText
    {
        get => _progressText;
        private set { _progressText = value; OnPropertyChanged(); }
    }
    public string CurrentApp
    {
        get => _currentApp;
        private set { _currentApp = value; OnPropertyChanged(); }
    }

    private bool _isBusy;
    private string _statusSummary = "Ready";
    private double _overallProgress;
    private string _progressText = "";
    private string _currentApp = "";
    private RebootManager? _reboot;

    public RelayCommand SelectAllCommand { get; }
    public RelayCommand DeselectAllCommand { get; }
    public RelayCommand InstallCommand { get; }
    public RelayCommand CancelRebootCommand { get; }
    public RelayCommand CloseCommand { get; }

    public Action? ShowRebootDialog;
    public Action<int, string>? UpdateRebootCountdown;
    public Action? HideRebootDialog;
    public Action? ShowSummary;
    public Action? CloseWindow;

    public MainWindowViewModel()
    {
        SelectAllCommand = new RelayCommand(_ =>
        {
            foreach (var a in Apps) a.IsSelected = true;
            RefreshSelectionState();
        });
        DeselectAllCommand = new RelayCommand(_ =>
        {
            foreach (var a in Apps) a.IsSelected = false;
            RefreshSelectionState();
        });
        InstallCommand = new RelayCommand(async _ => await InstallAsync(), _ => CanInstall);
        CancelRebootCommand = new RelayCommand(_ => _reboot?.Cancel(), _ => _reboot != null && _reboot.IsCountingDown);
        CloseCommand = new RelayCommand(_ => CloseWindow?.Invoke(), _ => !IsBusy && _reboot == null);
    }

    public void LoadApps(List<AppEntry> entries)
    {
        Apps.Clear();
        foreach (var e in entries)
        {
            var avm = new AppViewModel(e);
            avm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AppViewModel.IsSelected))
                {
                    RefreshSelectionState();
                    OnPropertyChanged(nameof(AllSelected));
                    OnPropertyChanged(nameof(NoneSelected));
                }
            };
            Apps.Add(avm);
        }
        RefreshSelectionState();
    }

    private void RefreshSelectionState()
    {
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(AllSelected));
        OnPropertyChanged(nameof(NoneSelected));
        InstallCommand?.RaiseCanExecuteChanged();
        CancelRebootCommand?.RaiseCanExecuteChanged();
        CloseCommand?.RaiseCanExecuteChanged();
    }

    private async Task InstallAsync()
    {
        IsBusy = true;
        StatusSummary = "Starting installation...";
        Log.Clear();
        _reboot = null;
        OverallProgress = 0;
        ProgressText = "";
        CurrentApp = "";
        RefreshSelectionState();

        try
        {
            var selected = Apps.Where(a => a.IsSelected).Select(a => a.Entry).ToList();
            var workingDir = Path.Combine(Path.GetTempPath(), "Eztaller", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingDir);

            var engine = new InstallerEngine();
            int total = selected.Count;
            int completed = 0;

            foreach (var app in selected)
            {
                var avm = Apps.First(a => a.Entry.Id == app.Id);
                avm.Status = InstallStatus.Pending;
                avm.StatusText = "Queued";

                CurrentApp = app.Name;
                ProgressText = $"Installing {completed + 1} of {total}...";
                OverallProgress = (double)completed / total * 100;

                var result = await InstallOneAsync(avm, app, workingDir);
                completed++;

                avm.Status = result.Status;
                avm.StatusText = result.Message;
                avm.IsInstalling = false;

                OverallProgress = (double)completed / total * 100;
                ProgressText = $"Completed {completed} of {total}";

                LogAdd($"[{result.Status}] {app.Name}: {result.Message}");
            }

            LogAdd(string.Empty);
            LogAdd("=== Installation complete ===");

            var failures = Apps.Where(a => a.Status == InstallStatus.Failed).ToList();
            var successes = Apps.Where(a => a.Status == InstallStatus.Completed).ToList();

            StatusSummary = failures.Count == 0
                ? $"All {successes.Count} app(s) installed successfully."
                : $"{successes.Count} installed, {failures.Count} failed.";

            if (failures.Count > 0)
            {
                var failList = string.Join("\n", failures.Select(f => $"- {f.Entry.Name}: {f.StatusText}"));
                StatusSummary += $"\n\nFailures:\n{failList}";
            }

            ShowSummary?.Invoke();

            CurrentApp = "";
            ProgressText = "Installation finished.";

            LogAdd("Rebooting in 60 seconds. Click Cancel Reboot to abort.");
            ShowRebootDialog?.Invoke();

            _reboot = new RebootManager();
            _reboot.CountdownTick += seconds => UpdateRebootCountdown?.Invoke(seconds, $"Restarting in {seconds} seconds...");
            _reboot.RebootCancelled += () =>
            {
                LogAdd("Reboot cancelled by user.");
                HideRebootDialog?.Invoke();
                StatusSummary = "Reboot cancelled. You can close the program.";
                IsBusy = false;
                _reboot = null;
                RefreshSelectionState();
            };
            _reboot.RebootInitiated += () =>
            {
                LogAdd("Rebooting now...");
                HideRebootDialog?.Invoke();
            };

            _reboot.StartCountdown(60, () => { });
        }
        catch (Exception ex)
        {
            LogAdd($"Fatal error: {ex.Message}");
            StatusSummary = "Installation stopped due to an error.";
            IsBusy = false;
            _reboot = null;
            RefreshSelectionState();
        }
        finally
        {
            if (!IsBusy) RefreshSelectionState();
        }
    }

    private async Task<InstallResult> InstallOneAsync(AppViewModel avm, AppEntry app, string workingDir)
    {
        avm.IsInstalling = true;
        avm.Status = InstallStatus.Pending;
        avm.StatusText = "Starting...";

        var engine = new InstallerEngine();
        var results = await engine.InstallAllAsync(new List<AppEntry> { app }, workingDir, new Progress<string>(msg => LogAdd(msg)), CancellationToken.None);

        return results.FirstOrDefault() ?? new InstallResult
        {
            App = app,
            Status = InstallStatus.Failed,
            Message = "No result returned.",
            Duration = TimeSpan.Zero
        };
    }

    private void LogAdd(string message)
    {
        Log.Add(message);
        OnPropertyChanged(nameof(Log));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}