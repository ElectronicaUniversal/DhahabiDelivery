using System.Text.Json;
using DhahabiDelivery.Modules.Shared.Models;

namespace DhahabiDelivery.Modules.Shared.Services;

public class StorageService : IStorageService
{
    /// <summary>
    ///     Removes a key and its associated value if it exists.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    void IStorageService.Remove(string key)
    {
        Preferences.Remove(key);
    }

    /// <summary>
    ///     Gets and decrypts the value for a given key.
    /// </summary>
    /// <param name="key">The key to retrieve the value for.</param>
    /// <returns>The decrypted string value or <see langword="null" /> if a value was not found.</returns>
    public string? GetAsync(string key)
    {
        return Preferences.Get(key, null);
    }

    public ModeloUsuario? GetActiveUser()
    {
        var user = Preferences.Get("USER_DATA", null);
        if (user == null) return null;
        try
        {
            return JsonSerializer.Deserialize<ModeloUsuario>(user);
        }
        catch
        {
            return null;
        }
    }

    public void SaveActiveUser(ModeloUsuario usuario)
    {
        try
        {
            var user = JsonSerializer.Serialize(usuario);
            SetAsync("USER_DATA", user);
        }
        catch (Exception)
        {
            //throw
        }
    }

    public void SetAsync<T>(string key, T value)
    {
        var res = JsonSerializer.Serialize(value);
        Preferences.Set(key, res);
        var result = Preferences.Get(key, null);
    }

    public T? GetAsync<T>(string key)
    {
        try
        {
            var res = Preferences.Get(key, null) ?? throw new KeyNotFoundException();
            return JsonSerializer.Deserialize<T>(res);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    ///     Sets and encrypts a value for a given key.
    /// </summary>
    /// <param name="key">The key to set the value for.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>A <see cref="Task" /> object with the current status of the asynchronous operation.</returns>
    public void SetAsync(string key, string value)
    {
        Preferences.Set(key, value);
    }
}