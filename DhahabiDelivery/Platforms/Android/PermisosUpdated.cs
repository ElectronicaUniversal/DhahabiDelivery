using Application = Android.App.Application;
#if ANDROID
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
#endif

namespace DhahabiDelivery;

public static class PermisosUpdated
{
    public static async Task<bool> RequestLocationPermissionsAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        if (status != PermissionStatus.Granted)
            return false;

        // Background (solo Android)
#if ANDROID
        var backgroundStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (backgroundStatus != PermissionStatus.Granted)
            backgroundStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();

        // Para Android 14 (API 34) y superior, solicitar permiso específico para servicios en primer plano
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
            return backgroundStatus == PermissionStatus.Granted; // API 33+
        var hasForegroundServicePermission = CheckForegroundServiceLocationPermission();
        if (!hasForegroundServicePermission)
        {
            RequestForegroundServiceLocationPermission();
            // No podemos saber inmediatamente si se concedió, así que volvemos a verificar
            hasForegroundServicePermission = CheckForegroundServiceLocationPermission();
        }

        if (!hasForegroundServicePermission) Console.WriteLine("⚠️ Permiso FOREGROUND_SERVICE_LOCATION no concedido");
        // No fallamos aquí porque este permiso solo es necesario en Android 14+
        // y puede que el usuario esté en una versión anterior
        return backgroundStatus == PermissionStatus.Granted;
#endif
        return false;
    }

#if ANDROID
    // Método para verificar el permiso FOREGROUND_SERVICE_LOCATION
    private static bool CheckForegroundServiceLocationPermission()
    {
        var context = Application.Context;
        return ContextCompat.CheckSelfPermission(context, "android.permission.FOREGROUND_SERVICE_LOCATION") ==
               Permission.Granted;
    }

    // Método para solicitar el permiso FOREGROUND_SERVICE_LOCATION
    private static void RequestForegroundServiceLocationPermission()
    {
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity != null)
            {
                ActivityCompat.RequestPermissions(
                    activity,
                    new[] { "android.permission.FOREGROUND_SERVICE_LOCATION" },
                    100); // código de solicitud arbitrario

                Console.WriteLine("📲 Solicitando permiso FOREGROUND_SERVICE_LOCATION");
            }
            else
            {
                Console.WriteLine("❌ No se puede solicitar FOREGROUND_SERVICE_LOCATION: actividad nula");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al solicitar permiso: {ex.Message}");
        }
    }
#endif
}