using Android.Content;
using Android.Locations;
using Android.Provider;
using Application = Android.App.Application;
using Context = Android.Content.Context;

namespace DhahabiDelivery;

/// <summary>
///     Utilidades para gestionar el GPS en Android
/// </summary>
public static class GpsUtils
{
    /// <summary>
    ///     Verifica si el GPS está habilitado en el dispositivo
    /// </summary>
    public static Task<bool> IsGpsEnabledAsync()
    {
        try
        {
            var locationManager =
                Application.Context.GetSystemService(Context.LocationService) as
                    LocationManager;
            return Task.FromResult(locationManager?.IsProviderEnabled(LocationManager.GpsProvider) ?? false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al verificar estado del GPS: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    ///     Abre la configuración del sistema para activar el GPS
    /// </summary>
    /// <returns>True si se pudo abrir la configuración</returns>
    public static Task<bool> RequestEnableGpsAsync()
    {
        try
        {
            var locationRequest = new Intent(Settings.ActionLocationSourceSettings);
            locationRequest.SetFlags(ActivityFlags.NewTask);
            Application.Context.StartActivity(locationRequest);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al solicitar activación del GPS: {ex.Message}");
            return Task.FromResult(false);
        }
    }
}