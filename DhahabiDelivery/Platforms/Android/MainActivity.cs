using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using DhahabiDelivery.Modules.Shared.Services;
using ME.Pushy.Sdk;

namespace DhahabiDelivery;

[Activity(Theme = "@style/DhahabiTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Check if our service is running and sync state properly
        var isServiceRunning = DeliveryTrackingServiceFixed.CheckIfServiceIsRunning();
        Console.WriteLine($"MainActivity onCreate - Service running: {isServiceRunning}");

        // Si el servicio está en ejecución, aseguramos que la aplicación lo sepa
        if (isServiceRunning)
            // Utilizamos el hilo principal para evitar problemas de threading
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Intentar recuperar el LocationService del contenedor de DI
                    var locationService = IPlatformApplication.Current?.Services.GetService<LocationService>();
                    if (locationService == null) return;
                    // Obtener el estado actual guardado en SharedPreferences
                    var prefs = GetSharedPreferences("delivery_prefs", FileCreationMode.Private);
                    var savedState = prefs?.GetString("delivery_state", null);

                    if (string.IsNullOrEmpty(savedState)) return;
                    // Sincronizar el estado con el servicio de ubicación
                    Console.WriteLine($"Sincronizando estado desde SharedPreferences: {savedState}");

                    // Actualizar el estado en el LocationService sin iniciar nuevamente el servicio
                    locationService.CurrentDeliveryState = savedState;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sincronizando estado con servicio: {ex.Message}");
                }
            });

        var registered = Preferences.Default.Get("registerd", false);
        if (!registered) Register();
        // Start listening for notifications
        Pushy.Listen(Platform.CurrentActivity);
    }

    private void Register()
    {
        // Execute Pushy.Register() in a background thread
        Task.Run(() =>
        {
            try
            {
                // Assign a unique token to this device
                var deviceToken = Pushy.Register(Platform.CurrentActivity);

                Log.Debug("Device Token", deviceToken ?? string.Empty);
                // Send the token to your backend server via an HTTP GET request
                //new URL("https://{YOUR_API_HOSTNAME}/register/device?token=" + deviceToken).OpenConnection();


                Pushy.Subscribe($"newsv{AppInfo.Current.BuildString}", Platform.CurrentActivity);
                Preferences.Default.Set("registerd", true);
            }
            catch (Exception exc)
            {
                // Log error to console
                Log.Error("MyApp", exc.Message, exc);
            }
        });
    }

    protected override void OnDestroy()
    {
        try
        {
            // Verificar si el servicio está ejecutándose y detenerlo
            var isServiceRunning = DeliveryTrackingServiceFixed.CheckIfServiceIsRunning();
            if (!isServiceRunning) return;
            Console.WriteLine("MainActivity OnDestroy - Deteniendo servicio de localización");
            DeliveryTrackingServiceFixed.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al detener servicio en OnDestroy: {ex.Message}");
        }
        finally
        {
            base.OnDestroy();
        }
    }
}