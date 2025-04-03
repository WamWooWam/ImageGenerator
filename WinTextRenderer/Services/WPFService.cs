using System.Windows;
using System.Windows.Threading;

namespace WinTextRenderer.Services;

public class WPFService(ILogger<WPFService> logger, DispatcherAccessor dispatcherAccessor) : IHostedService
{
    private Thread? _wpfThread;
    private Application? _application;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _wpfThread = new Thread(WpfThreadProc);
        _wpfThread.SetApartmentState(ApartmentState.STA);
        _wpfThread.Start();

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_application != null)
        {
            await _application.Dispatcher.InvokeAsync(_application.Shutdown);
        }
    }

    private void WpfThreadProc()
    {
        var application = new Application();
        application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        application.Startup += OnApplicationStartup;
        application.DispatcherUnhandledException += OnUnhandledException;
        application.Run();
    }

    private void OnApplicationStartup(object sender, StartupEventArgs e)
    {
        _application = (Application)sender;
        dispatcherAccessor.Dispatcher = _application.Dispatcher;
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        logger.LogError(e.Exception, "Exception on WPF Dispatcher, this is bad!");
    }
}