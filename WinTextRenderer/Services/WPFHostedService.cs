using System.Windows;
using System.Windows.Threading;

namespace WinTextRenderer.Services;

public class WPFHostedService(ILogger<WPFHostedService> logger,
                              IDispatcher dispatcher) : IHostedService
{
    private Thread? _wpfThread;
    private Application? _application;
    private readonly WPFDispatcher _wpfDispatcher = (WPFDispatcher)dispatcher;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _wpfThread = new Thread(WpfThreadProc);
        _wpfThread.SetApartmentState(ApartmentState.STA);
        _wpfThread.Start();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _wpfDispatcher.Shutdown();

        return Task.CompletedTask;
    }

    private void WpfThreadProc()
    {
        var application = new Application();
        application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        application.Startup += OnApplicationStartup;
        application.Run();
    }

    private void WpfWorkerThreadProc()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        _wpfDispatcher.AddDispatcher(dispatcher);
        Dispatcher.Run();
    }

    private void OnApplicationStartup(object sender, StartupEventArgs e)
    {
        _application = (Application)sender;
        _wpfDispatcher.AddDispatcher(_application.Dispatcher);
        logger.LogInformation("WPF Application started.");

        // WPF text rendering seems to be unable to scale past 4 threads, so limit it to 4.
        var threads = Math.Clamp(Environment.ProcessorCount, 0, 4);
        logger.LogInformation("Creating {x} dispatchers for {y} CPU threads.", threads + 1, Environment.ProcessorCount);

        for (int i = 0; i < threads; i++)
        {
            var workerThread = new Thread(WpfWorkerThreadProc);
            workerThread.SetApartmentState(ApartmentState.STA);
            workerThread.Start();
        }
    }
}