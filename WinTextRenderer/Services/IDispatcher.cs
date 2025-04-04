namespace WinTextRenderer.Services;

public interface IDispatcher
{
    public Task InvokeAsync(Action work);
    public Task<T> InvokeAsync<T>(Func<T> work);
}
