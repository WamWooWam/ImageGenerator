
using System.Windows.Threading;

namespace WinTextRenderer.Services;

public class WPFDispatcher(ILogger<WPFDispatcher> logger) : IDispatcher
{
    private int i = 0;
    private readonly List<Dispatcher> _dispatchers = [];

    public async Task InvokeAsync(Action work)
    {
        var idx = Interlocked.Increment(ref i) % _dispatchers.Count;
        var dispatcher = _dispatchers[idx];
        await dispatcher.InvokeAsync(work);
    }

    public async Task<T> InvokeAsync<T>(Func<T> work)
    {
        var idx = Interlocked.Increment(ref i) % _dispatchers.Count;
        var dispatcher = _dispatchers[idx];
        return await dispatcher.InvokeAsync(work);
    }

    public void AddDispatcher(Dispatcher dispatcher)
    {
        lock (_dispatchers)
        {
            _dispatchers.Add(dispatcher);
        }

        dispatcher.UnhandledException += OnUnhandledException;
        dispatcher.ShutdownFinished += OnShutdown;
        dispatcher.Invoke(() => logger.LogInformation("Dispatcher starting up."));
    }

    public void Shutdown()
    {
        lock (_dispatchers)
        {
            foreach (var dispatcher in _dispatchers)
                dispatcher.InvokeShutdown();
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        logger.LogError(e.Exception, "Exception on WPF Dispatcher, this is bad!");
    }

    private void OnShutdown(object? sender, EventArgs e)
    {
        logger.LogInformation("Dispatcher shutting down.");
    }
}
