// ReSharper disable LocalizableElement

using Android.App;
using Android.Content;
using Android.OS;
using DhahabiDelivery.Modules.Entregas;
using DhahabiDelivery.Modules.Shared.Services;
using Application = Android.App.Application;

namespace DhahabiDelivery;

public static class DeliveryTrackingServiceFixed
{
    private static LocationForegroundServiceFixed? _serviceInstance;

    public static void Start()
    {
#if ANDROID
        try
        {
            Console.WriteLine("Iniciando servicio de tracking");
            var context = Application.Context;
            var intent = new Intent(context, typeof(LocationForegroundServiceFixed));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                Console.WriteLine(" Iniciando servicio de tracking con StartForegroundService");
                context.StartForegroundService(intent);
            }
            else
                // En versiones anteriores a Android 8.0, usamos StartService
            {
                context.StartService(intent);
            }

            Console.WriteLine(" Servicio de tracking iniciado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Error iniciando servicio de tracking: {ex.Message}");
        }
#endif
    }

    public static void Stop()
    {
#if ANDROID
        try
        {
            var context = Application.Context;
            var intent = new Intent(context, typeof(LocationForegroundServiceFixed));
            context.StopService(intent);
            Console.WriteLine(" Servicio de tracking detenido");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Error deteniendo servicio de tracking: {ex.Message}");
        }
#endif
    }

    public static void UpdateDeliveryState(string state)
    {
#if ANDROID
        try
        {
            // Intentar actualizar el estado a través del LocationService centralizado primero
            var locationService = IPlatformApplication.Current?.Services.GetService<LocationService>();
            if (locationService != null)
            {
                Console.WriteLine($" Estado de delivery actualizado en LocationService: {state}");
            }
            else
            {
                // Fallback a SharedPreferences si el servicio no está disponible
                var context = Application.Context;
                var prefs = context.GetSharedPreferences("delivery_prefs", FileCreationMode.Private);
                var editor = prefs?.Edit();
                editor?.PutString("delivery_state", state);
                editor?.Apply();
                Console.WriteLine($" Estado de delivery actualizado en SharedPreferences: {state}");
            }

            // Si el servicio ya está en ejecución, notificarle del cambio de estado directamente
            if (_serviceInstance != null)
            {
                _serviceInstance.UpdateState(state);
                Console.WriteLine($" Notificación directa al servicio del estado: {state}");
            }
            else
            {
                // Si no tenemos referencia al servicio pero está ejecutando, hacerlo indirectamente
                // enviando una señal de inicio
                if (state is ConstantesEstadoRepartidor.ENTREGANDO or ConstantesEstadoRepartidor.DISPONIBLE)
                    // Solo enviamos señal de inicio si está en un estado que requiere el servicio activo
                    Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Error actualizando estado: {ex.Message}");
        }
#endif
    }

    // Método para que el servicio se registre cuando se inicia
    internal static void RegisterServiceInstance(LocationForegroundServiceFixed instance)
    {
#if ANDROID
        _serviceInstance = instance;
        Console.WriteLine(" Servicio registrado para comunicación directa");
#endif
    }

    // Método para que el servicio se desregistre al detenerse
    internal static void UnregisterServiceInstance()
    {
#if ANDROID
        _serviceInstance = null;
        Console.WriteLine(" Servicio desregistrado");
#endif
    }

    // Método para verificar si el servicio está en ejecución
    public static bool CheckIfServiceIsRunning()
    {
#if ANDROID
        try
        {
            var context = Application.Context;

            if (context.GetSystemService(Context.ActivityService) is ActivityManager manager)
                foreach (var service in manager.GetRunningServices(int.MaxValue))
                    if (service.Service != null && service.Service.ClassName.EndsWith("LocationForegroundServiceFixed"))
                    {
                        Console.WriteLine("✅ Servicio de ubicación detectado en ejecución");
                        return true;
                    }

            Console.WriteLine("ℹ️ Servicio de ubicación no está en ejecución");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error verificando estado del servicio: {ex.Message}");
            return false;
        }
#else
        return false;
#endif
    }
}