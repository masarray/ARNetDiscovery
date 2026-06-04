using System.Windows;
using System.Windows.Threading;
using ARNetDiscovery.Wpf.ViewModels;

namespace ARNetDiscovery.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static MainViewModel? TryGetViewModelOnUiThread()
        => Current?.MainWindow?.DataContext as MainViewModel;

    private static void PublishUnhandledException(string source, Exception exception)
    {
        var app = Current;
        var dispatcher = app?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return;

        void Publish()
        {
            try
            {
                TryGetViewModelOnUiThread()?.ReportUnhandledException(source, exception);
            }
            catch
            {
                // Last-resort application guard: never throw while reporting an exception.
            }
        }

        if (dispatcher.CheckAccess())
        {
            Publish();
        }
        else
        {
            dispatcher.BeginInvoke((Action)Publish, DispatcherPriority.Background);
        }
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
}
