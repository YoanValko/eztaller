using System;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text.Json;
using System.Windows;

namespace AppInstaller;

public partial class WingetApp : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!IsRunningAsAdmin())
        {
            MessageBox.Show(
                "This program needs administrator rights to install applications and restart the PC. " +
                "Please re-launch it as administrator.",
                "Administrator rights required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Current.Shutdown();
            return;
        }

        var manifest = LoadManifest();
        if (manifest == null || manifest.Apps.Count == 0)
        {
            MessageBox.Show(
                "Failed to load the application list (apps.json). Make sure it is next to the program.",
                "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
            return;
        }

        var vm = new MainWindowViewModel();
        vm.LoadApps(manifest.Apps);

        var mainWindow = new MainWindow(vm);
        mainWindow.Show();
    }

    private static bool IsRunningAsAdmin()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static AppManifest? LoadManifest()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var paths = new[]
            {
                Path.Combine(baseDir, "apps.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps.json"),
                Path.GetFullPath("apps.json")
            };

            string? json = null;
            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    json = File.ReadAllText(p);
                    break;
                }
            }

            if (json == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "WingetInstaller.apps.json";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return null;
                using var reader = new StreamReader(stream);
                json = reader.ReadToEnd();
            }

            return JsonSerializer.Deserialize<AppManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }
}