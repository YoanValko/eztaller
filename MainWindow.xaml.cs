using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AppInstaller;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;

    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;

        _vm.ShowRebootDialog = () =>
        {
            Dispatcher.Invoke(() => RebootPopup.IsOpen = true);
        };
        _vm.HideRebootDialog = () =>
        {
            Dispatcher.Invoke(() => RebootPopup.IsOpen = false);
        };
        _vm.UpdateRebootCountdown = (seconds, text) =>
        {
            Dispatcher.Invoke(() =>
            {
                CountdownSeconds.Text = seconds.ToString();
            });
        };
        _vm.ShowSummary = () =>
        {
        };
        _vm.CloseWindow = Close;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && RebootPopup.IsOpen)
        {
            _vm.CancelRebootCommand.Execute(null);
            e.Handled = true;
        }
    }
}