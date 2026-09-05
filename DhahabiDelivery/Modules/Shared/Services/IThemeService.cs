namespace DhahabiDelivery.Modules.Shared.Services;

public interface IThemeService
{
    Task<bool> GetIsDarkAsync();
    Task SetIsDarkAsync(bool isDark);
    Task ApplyStoredThemeAsync();
}
