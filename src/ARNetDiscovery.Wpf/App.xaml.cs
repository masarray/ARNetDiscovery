using System.IO;
using System.Windows;
using System.Windows.Threading;
using ARNetDiscovery.Wpf.ViewModels;

namespace ARNetDiscovery.Wpf;

public partial class App : Application
{
    private static readonly object LogLock = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        // Register guards before any UI or ViewModel is created. In previous builds, a startup
        // exception could be handled by the guard before MainWindow existed, resulting in a
        // successful build/run with no visible application window.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        LogStartup("Application startup requested.");

        try
        {
            var window = new MainWindow();
            var viewModel = new MainViewModel();
            window.DataContext = viewModel;
            MainWindow = window;
            window.Show();
            window.Activate();
            LogStartup("MainWindow shown successfully.");
        }
        catch (Exception ex)
        {
            LogStartup("Startup failed: " + ex);
            ShowStartupFailure(ex);
            Shutdown(-1);
        }
    }

    private static MainViewModel? TryGetViewModelOnUiThread()
        => Current?.MainWindow?.DataContext as MainViewModel;

    private static void PublishUnhandledException(string source, Exception exception)
    {
        LogStartup($"{source} exception: {exception}");

        var app = Current;
        var dispatcher = app?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return;

        void Publish()
        {
            try
            {
                var vm = TryGetViewModelOnUiThread();
                if (vm is not null)
                {
                    vm.ReportUnhandledException(source, exception);
                    return;
                }

                // If this happens during early startup, there is no diagnostics panel yet.
                // Show a small explicit message instead of leaving the user with no window.
                if (app?.MainWindow is null)
                    ShowStartupFailure(exception);
            }
            catch
            {
                // Last-resort application guard: never throw while reporting an exception.
            }
        }

        if (dispatcher.CheckAccess())
            Publish();
        else
            dispatcher.BeginInvoke((Action)Publish, DispatcherPriority.Background);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        PublishUnhandledException("Dispatcher", e.Exception);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            PublishUnhandledException("AppDomain", ex);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        PublishUnhandledException("TaskScheduler", e.Exception);
        e.SetObserved();
    }

    private static void ShowStartupFailure(Exception ex)
    {
        try
        {
            MessageBox.Show(
                "ARNet Discovery could not open the main window.\n\n" +
                ex.GetType().Name + ": " + ex.Message + "\n\n" +
                "A startup log was written to:\n" + GetStartupLogPath(),
                "ARNet Discovery startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // Ignore if MessageBox itself is unavailable.
        }
    }

    private static void LogStartup(string message)
    {
        try
        {
            var path = GetStartupLogPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            lock (LogLock)
            {
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never block application startup.
        }
    }

    private static string GetStartupLogPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "ARNetDiscovery", "startup.log");
    }
}
