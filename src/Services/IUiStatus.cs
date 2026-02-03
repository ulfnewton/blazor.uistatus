
namespace DemoBlazorServerNetwork.Services;

public record UiError(string Title, string Detail);

public interface IUiStatus
{
    bool IsBusy { get; }
    bool IsOffline { get; }
    UiError? Error { get; }

    event Action? Changed;

    void Busy(bool on);
    void Offline(bool on);
    void SetError(UiError? error);
    void Notify();
}
