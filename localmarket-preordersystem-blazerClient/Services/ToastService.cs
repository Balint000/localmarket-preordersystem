namespace localmarket_preordersystem.Services;

public enum ToastKind { Success, Error, Info }

public sealed record Toast(Guid Id, ToastKind Kind, string Message);

/// <summary>Értesítések (toast) egy körfolyamaton belül; a ToastHost komponens jeleníti meg.</summary>
public sealed class ToastService
{
    public event Action? Changed;
    private readonly List<Toast> _items = [];
    public IReadOnlyList<Toast> Items => _items;

    public void Success(string message) => Add(ToastKind.Success, message);
    public void Error(string message) => Add(ToastKind.Error, message);
    public void Info(string message) => Add(ToastKind.Info, message);

    public void Dismiss(Guid id)
    {
        if (_items.RemoveAll(t => t.Id == id) > 0) Changed?.Invoke();
    }

    private void Add(ToastKind kind, string message)
    {
        var toast = new Toast(Guid.NewGuid(), kind, message);
        _items.Add(toast);
        Changed?.Invoke();
        _ = AutoDismissAsync(toast.Id, kind == ToastKind.Error ? 8000 : 4500);
    }

    private async Task AutoDismissAsync(Guid id, int ms)
    {
        await Task.Delay(ms);
        Dismiss(id);
    }
}
