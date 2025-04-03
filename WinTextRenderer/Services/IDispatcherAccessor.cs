using System.Windows.Threading;

namespace WinTextRenderer.Services;

public class DispatcherAccessor
{
    public Dispatcher? Dispatcher { get; set; }
}
