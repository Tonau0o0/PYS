using CommunityToolkit.Mvvm.ComponentModel;
using PYS.Client.Services;

namespace PYS.Client.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    protected void HandleException(Exception ex)
    {
        App.LogException(GetType().Name, ex);

        if (ex is ApiException api)
        {
            ErrorMessage = api.Details.Length > 0
                ? $"{api.Message} ({string.Join("; ", api.Details)})"
                : api.Message;
        }
        else if (ex is HttpRequestException)
        {
            ErrorMessage = "API'ye ulaşılamıyor. Backend çalışıyor mu? (http://localhost:5014)";
        }
        else
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>
    /// Starts an async operation from the current thread (typically the UI thread when invoked
    /// from a property-changed handler) and swallows any unobserved exception into the UI as a
    /// readable error. Critically we DO NOT use Task.Run here — that would marshal subsequent
    /// awaits onto the thread pool and ObservableCollection mutations would race the WinUI
    /// Dispatcher (RPC_E_WRONG_THREAD / 0x8001010E).
    /// </summary>
    protected void FireAndForget(Func<Task> work)
    {
        _ = SafeAsync(work);
    }

    private async Task SafeAsync(Func<Task> work)
    {
        try { await work(); }
        catch (Exception ex)
        {
            if (MainThread.IsMainThread) HandleException(ex);
            else MainThread.BeginInvokeOnMainThread(() => HandleException(ex));
        }
    }
}
