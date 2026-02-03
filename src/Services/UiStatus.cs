
namespace DemoBlazorServerNetwork.Services;

public sealed class UiStatus : IUiStatus
{
    public bool IsBusy { get; private set; }
    public bool IsOffline { get; private set; }
    public UiError? Error { get; private set; }
    public event Action? Changed;

    public void Busy(bool on)
    {
        IsBusy = on;
        Changed?.Invoke();
    }

    public void Offline(bool on)
    {
        IsOffline = on;
        Changed?.Invoke();
    }

    public void SetError(UiError? error)
    {
        Error = error;
        Changed?.Invoke();
    }

    public void Notify() => Changed?.Invoke();
}
