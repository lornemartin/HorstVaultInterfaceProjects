using Microsoft.AspNetCore.Components;

namespace HorstMFG.Web.Components;

/// <summary>
/// Wraps an event handler so Blazor doesn't re-render the component after it runs.
/// Use for high-frequency events (e.g. keystrokes) on pages where a full render is
/// expensive; the handler calls StateHasChanged itself when it has something to show.
/// </summary>
public static class EventUtil
{
    public static Func<TValue, Task> AsNonRenderingEventHandler<TValue>(Func<TValue, Task> callback)
        => new AsyncReceiver<TValue>(callback).Invoke;

    private record AsyncReceiver<T>(Func<T, Task> Callback) : ReceiverBase
    {
        public Task Invoke(T arg) => Callback(arg);
    }

    private record ReceiverBase : IHandleEvent
    {
        public Task HandleEventAsync(EventCallbackWorkItem item, object? arg) => item.InvokeAsync(arg);
    }
}
