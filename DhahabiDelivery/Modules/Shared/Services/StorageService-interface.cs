using DhahabiDelivery.Modules.Shared.Models;

namespace DhahabiDelivery.Modules.Shared.Services;

public interface IStorageService
{
    void SetAsync(string key, string value);
    void Remove(string key);
    string? GetAsync(string key);
    ModeloUsuario? GetActiveUser();
    void SaveActiveUser(ModeloUsuario usuario);
    T? GetAsync<T>(string key);
    void SetAsync<T>(string key, T value);

    public static class Constantes
    {
        public const string CurrentCulture = "CURRENT_CULTURE";
    }
}