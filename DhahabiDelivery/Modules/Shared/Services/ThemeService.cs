using Microsoft.JSInterop;

namespace DhahabiDelivery.Modules.Shared.Services;

public class ThemeService(IJSRuntime jsRuntime) : IThemeService, IAsyncDisposable
{
    private IJSObjectReference? _module;

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        return _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/theme.js");
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
        if (_module != null) await _module.DisposeAsync();
    }
}
