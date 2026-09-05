using Microsoft.JSInterop;

namespace DhahabiDelivery.Modules.Shared.Services;

public class ThemeService(IJSRuntime jsRuntime) : IThemeService, IAsyncDisposable
{
    private Task<IJSObjectReference>? _modulePromise;

    private Task<IJSObjectReference> GetModuleAsync()
    {
        return _modulePromise ??= jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/theme.js").AsTask();
    }

    public async Task<bool> GetIsDarkAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool>("getIsDark");
    }

    public async Task SetIsDarkAsync(bool isDark)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("setIsDark", isDark);
    }

    public async Task ApplyStoredThemeAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("applyStoredTheme");
    }

    public async ValueTask DisposeAsync()
    {
        if (_modulePromise != null)
        {
            var module = await _modulePromise;
            await module.DisposeAsync();
        }
    }
}
