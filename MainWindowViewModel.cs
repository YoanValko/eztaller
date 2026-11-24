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
        set { _isInstalling = value; OnPropertyChanged(); }
    }
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    private bool _isSelected;
    private bool _isInstalling;
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

    private bool _isBusy;
    private string _statusSummary = "Ready";
    private RebootManager? _reboot;

    public RelayCommand SelectAllCommand { get; }
    public RelayCommand DeselectAllCommand { get; }
    public RelayCommand InstallCommand { get; }
    public RelayCommand CancelRebootCommand { get; }

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
                    RefreshSelectionState();
            };
            Apps.Add(avm);
        }
        RefreshSelectionState();
    }

    private void RefreshSelectionState()
    {
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(SelectedCount));
        InstallCommand?.RaiseCanExecuteChanged();
        CancelRebootCommand?.RaiseCanExecuteChanged();
    }

    private async Task InstallAsync()
    {
        IsBusy = true;
        StatusSummary = "Starting installation...";
        Log.Clear();
        _reboot = null;
        RefreshSelectionState();

        try
        {
            var selected = Apps.Where(a => a.IsSelected).Select(a => a.Entry).ToList();
            var workingDir = Path.Combine(Path.GetTempPath(), "WingetInstaller", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingDir);

            var engine = new InstallerEngine();
            var results = await engine.InstallAllAsync(selected, workingDir, new Progress<string>(msg => LogAdd(msg)), CancellationToken.None);

            LogAdd(string.Empty);
            LogAdd("=== Installation complete ===");

            var failures = results.Where(r => r.Status == InstallStatus.Failed).ToList();
            var successes = results.Where(r => r.Status == InstallStatus.Completed).ToList();

            foreach (var r in results)
            {
                var mark = r.Status == InstallStatus.Completed ? "[OK]" : "[FAIL]";
                LogAdd($"{mark} {r.App.Name}: {r.Message}");
            }

            StatusSummary = failures.Count == 0
                ? $"All {successes.Count} app(s) installed successfully."
                : $"{successes.Count} installed, {failures.Count} failed.";

            if (failures.Count > 0)
            {
                var failList = string.Join("\n", failures.Select(f => $"- {f.App.Name}: {f.Message}"));
                StatusSummary += $"\n\nFailures:\n{failList}";
            }

            ShowSummary?.Invoke();

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

    private void LogAdd(string message)
    {
        Log.Add(message);
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