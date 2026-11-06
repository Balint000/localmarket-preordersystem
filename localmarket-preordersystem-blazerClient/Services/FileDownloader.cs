using Microsoft.JSInterop;

namespace localmarket_preordersystem.Services;

public sealed class FileDownloader(IJSRuntime js)
{
    public ValueTask SaveAsync(DownloadedFile file)
        => js.InvokeVoidAsync("lmDownloadFile", file.FileName, file.ContentType, Convert.ToBase64String(file.Content));
}
