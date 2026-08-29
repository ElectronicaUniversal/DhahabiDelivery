using System.Diagnostics.CodeAnalysis;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using DhahabiDelivery.Modules.Entregas;
using DhahabiDelivery.Modules.Shared.Services;
using Java.Lang;
using Application = Android.App.Application;
using Exception = System.Exception;
using Location = Android.Locations.Location;

// ReSharper disable LocalizableElement

namespace DhahabiDelivery;

[Service(
    ForegroundServiceType = ForegroundService.TypeLocation,
    Exported = false
)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class LocationForegroundServiceFixed : Service, ILocationListener
{
    private const int SERVICE_ID = 1001;
    private const string CHANNEL_ID = "delivery_channel";
    private const string CHANNEL_NAME = "Servicio de Ubicación";
    private const string CHANNEL_DESCRIPTION = "Notificaciones del servicio de ubicación para entregas";
    private string _currentDeliveryState = ConstantesEstadoRepartidor.NO_DISPONIBLE;

    private bool _isLocationUpdatesActive;
    private Handler? _locationHandler;
    private LocationManager? _locationManager;
    private LocationService? _locationService;

    // Nuevo: hilo dedicado para ubicación
    private HandlerThread? _locationThread;

    public void OnLocationChanged(Location location)
    {
        try
        {
            // Procesamos la ubicación en el hilo dedicado para evitar bloquear el hilo principal
            _locationHandler?.Post(() =>
            {
                try
                {
                    switch (_currentDeliveryState)
                    {
                        // Solo enviar ubicación si está en estado entregando
                        case ConstantesEstadoRepartidor.ENTREGANDO:
                            Console.WriteLine(
                                $"📍 Posición actual (Entregando): {location.Latitude:F6}, {location.Longitude:F6}");
                            Console.WriteLine($"📊 Precisión: {location.Accuracy}m | Proveedor: {location.Provider}");

                            // Actualizar el servicio de ubicación compartido
                            if (location.Provider != null)
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    _locationService?.UpdateLocation(location.Latitude, location.Longitude,
                                        location.Accuracy,
                                        location.Provider);
                                });

                            // Enviar al backend
                            break;
                        case ConstantesEstadoRepartidor.DISPONIBLE:
                            Console.WriteLine(
                                $"📍 Posición actual (Disponible): {location.Latitude:F6}, {location.Longitude:F6}");

                            // Actualizar el servicio de ubicación compartido, pero sin enviar al backend
                            if (location.Provider != null)
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    _locationService?.UpdateLocation(location.Latitude, location.Longitude,
                                        location.Accuracy,
                                        location.Provider);
                                });
                            break;
                        default:
                            Console.WriteLine(
                                $"📍 Posición registrada (Estado: {_currentDeliveryState}): {location.Latitude:F6}, {location.Longitude:F6}");
                            break;
                    }
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"❌ Error procesando ubicación en hilo dedicado: {innerEx.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error procesando ubicación: {ex.Message}");
        }
    }

    public void OnProviderDisabled(string provider)
    {
    }

    public void OnProviderEnabled(string provider)
    {
        Console.WriteLine($"✅ Proveedor de ubicación habilitado: {provider}");
    }

    public void OnStatusChanged(string? provider, Availability status, Bundle? extras)
    {
        Console.WriteLine($"📡 Estado del proveedor {provider}: {status}");
    }

    public override void OnCreate()
    {
        base.OnCreate();

        // Crear hilo dedicado para ubicación
        _locationThread = new HandlerThread("LocationThread");
        _locationThread.Start();
        if (_locationThread.Looper != null) _locationHandler = new Handler(_locationThread.Looper);

        _locationManager = GetSystemService(LocationService) as LocationManager;

        // Obtener instancia del servicio de ubicación compartido
        _locationService = IPlatformApplication.Current?.Services.GetService<LocationService>();
        Console.WriteLine(_locationService == null
            ? "⚠️ No se pudo obtener el servicio de ubicación compartido"
            : "✅ Servicio de ubicación compartido obtenido correctamente");

        // Crear canal de notificación para Android 8.0+
        CreateNotificationChannel();

        Console.WriteLine("🟢 Servicio de ubicación creado");
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        try
        {
            // Registrar esta instancia para comunicación directa
            DeliveryTrackingServiceFixed.RegisterServiceInstance(this);

            // Verificar si es una acción para detener el servicio
            if (intent?.Action == "STOP_SERVICE")
            {
                StopSelf();
                return StartCommandResult.Sticky;
            }

            // Obtener el estado actual del delivery desde el servicio compartido
            _currentDeliveryState = _locationService?.CurrentDeliveryState ?? GetCurrentDeliveryState();

            Console.WriteLine($"📊 Estado de entrega al iniciar servicio: {_currentDeliveryState}");

            // Verificar primero que todos los permisos estén disponibles
            if (!HasLocationPermissions())
            {
                Console.WriteLine("❌ Error: Permisos de ubicación no concedidos. No se puede iniciar el servicio.");
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            try
            {
                // Crear notificación persistente mejorada
                var notification = CreateNotification();
                StartForeground(SERVICE_ID, notification);
            }
            catch (SecurityException secEx)
            {
                Console.WriteLine($"❌ Error de seguridad al iniciar servicio en primer plano: {secEx.Message}");
                Console.WriteLine("⚠️ Es posible que el permiso FOREGROUND_SERVICE_LOCATION no esté concedido.");

                // No podemos continuar sin este permiso en Android 14+
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            // Verificar permisos
            if (HasLocationPermissions())
            {
                // Actualizar el seguimiento de ubicación según el estado actual
                UpdateLocationTracking();
                Console.WriteLine("🟢 Servicio de ubicación iniciado correctamente");
            }
            else
            {
                Console.WriteLine("❌ Permisos de ubicación no concedidos");
                // No detenemos el servicio, solo mostramos la notificación sin escuchar ubicación
            }

            return StartCommandResult.Sticky;
        }
        catch (SecurityException secEx)
        {
            Console.WriteLine($"❌ Error de seguridad: {secEx.Message}");
            Console.WriteLine("⚠️ Verifica que el permiso FOREGROUND_SERVICE_LOCATION esté concedido para Android 14+");
            return StartCommandResult.NotSticky;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error iniciando servicio: {ex.Message}");
            return StartCommandResult.NotSticky;
        }
    }

    private static void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var channel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High);
        channel.Description = CHANNEL_DESCRIPTION;

        channel.SetShowBadge(false);
        channel.EnableLights(true);
        channel.EnableVibration(true);

        var notificationManager = Application.Context.GetSystemService(NotificationService) as NotificationManager;
        notificationManager?.CreateNotificationChannel(channel);
    }

    private Notification CreateNotification()
    {
        // Personalizar notificación según el estado
        var title = _currentDeliveryState == ConstantesEstadoRepartidor.ENTREGANDO
            ? "🚚 Dhahabi Delivery - Entregando"
            : "🚚 Dhahabi Delivery - Disponible";

        var text = _currentDeliveryState == ConstantesEstadoRepartidor.ENTREGANDO
            ? "Rastreando ubicación durante la entrega"
            : "Disponible para recibir entregas";

        var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
            .SetContentTitle(title)
            .SetContentText(text)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate)
            .SetCategory(NotificationCompat.CategoryService)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetPriority(NotificationCompat.PriorityHigh);

        // Agregar acción para detener el servicio
        var stopIntent = new Intent(this, typeof(LocationForegroundServiceFixed));
        stopIntent.SetAction("STOP_SERVICE");
        var stopPendingIntent = PendingIntent.GetService(this, 0, stopIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        builder.AddAction(0, "Detener", stopPendingIntent);

        return builder.Build();
    }

    private bool HasLocationPermissions()
    {
        var hasBasicLocationPermissions =
            ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted &&
            ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) == Permission.Granted;

        // Android 14 (API 34) o superior requiere este permiso para servicios en primer plano que usan ubicación
        if (Build.VERSION.SdkInt < BuildVersionCodes.UpsideDownCake)
            return hasBasicLocationPermissions; // API 34 = Android 14 (U)
        var hasForegroundLocationServicePermission =
            ContextCompat.CheckSelfPermission(this, "android.permission.FOREGROUND_SERVICE_LOCATION") ==
            Permission.Granted;

        if (hasForegroundLocationServicePermission) return hasBasicLocationPermissions;
        Console.WriteLine("❌ Falta permiso FOREGROUND_SERVICE_LOCATION requerido para Android 14");
        return false;
    }

    // Nuevo método para gestionar el seguimiento de ubicación según estado
    private void UpdateLocationTracking()
    {
        try
        {
            // Si ya estaba escuchando ubicaciones, primero detener
            if (_isLocationUpdatesActive && _locationManager != null)
            {
                _locationManager.RemoveUpdates(this);
                _isLocationUpdatesActive = false;
                Console.WriteLine("📍 Seguimiento de ubicación detenido");
            }

            // Activar escucha en los siguientes casos:
            // 1. Si está entregando: siempre activar para mandar al backend
            // 2. Si está disponible: activar con menor frecuencia para mantener la última posición conocida
            if (_currentDeliveryState is ConstantesEstadoRepartidor.ENTREGANDO or ConstantesEstadoRepartidor.DISPONIBLE)
            {
                RequestLocationUpdates();
                _isLocationUpdatesActive = true;
                Console.WriteLine($"📍 Seguimiento de ubicación ACTIVADO - Modo {_currentDeliveryState}");
            }
            else
            {
                Console.WriteLine($"📍 No se activa seguimiento GPS porque estado = {_currentDeliveryState}");
            }
        }
        catch (SecurityException ex)
        {
            Console.WriteLine($"❌ Error de permisos al gestionar actualizaciones: {ex.Message}");
        }
    }

    private void RequestLocationUpdates()
    {
        try
        {
            if (_locationManager == null || _locationHandler == null) return;

            // Configuración según el estado
            // 5 seg en entrega, 30 seg en disponible
            long intervaloMs = _currentDeliveryState == ConstantesEstadoRepartidor.ENTREGANDO ? 10000 : 30000;

            // 10 metros en entrega, 50 en disponible
            float distanciaMin = _currentDeliveryState == ConstantesEstadoRepartidor.ENTREGANDO ? 10 : 50;

            // Solicitar actualizaciones del GPS (alta precisión)
            if (_locationManager.IsProviderEnabled(LocationManager.GpsProvider))
            {
                _locationManager.RequestLocationUpdates(LocationManager.GpsProvider, intervaloMs, distanciaMin, this,
                    _locationThread?.Looper);

                Console.WriteLine(
                    $"📡 GPS activado: actualización cada {intervaloMs / 1000} segundos o {distanciaMin} metros");
            }

            // Solicitar actualizaciones de la red como respaldo
            // if (!_locationManager.IsProviderEnabled(LocationManager.NetworkProvider)) return;
            // _locationManager.RequestLocationUpdates(
            //     LocationManager.NetworkProvider,
            //     intervaloMs * 2, // Menos frecuente que GPS
            //     distanciaMin * 2,
            //     this, _locationThread?.Looper);
            //
            // Console.WriteLine(
            //     $"📡 Red activada como respaldo: actualización cada {intervaloMs * 2 / 1000} segundos");
        }
        catch (SecurityException ex)
        {
            Console.WriteLine($"❌ Error de permisos al solicitar actualizaciones: {ex.Message}");
        }
    }

    private string GetCurrentDeliveryState()
    {
        try
        {
            // Obtener estado desde SharedPreferences
            var prefs = GetSharedPreferences("delivery_prefs", FileCreationMode.Private);
            return prefs?.GetString("delivery_state", ConstantesEstadoRepartidor.NO_DISPONIBLE) ??
                   ConstantesEstadoRepartidor.NO_DISPONIBLE;
        }
        catch
        {
            return ConstantesEstadoRepartidor.NO_DISPONIBLE;
        }
    }

    // Método público para actualizar el estado desde DeliveryTrackingService
    public void UpdateState(string newState)
    {
        try
        {
            _currentDeliveryState = newState;
            Console.WriteLine($"📱 Estado actualizado a: {newState}");

            // Actualizar la notificación
            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.Notify(SERVICE_ID, CreateNotification());

            // Actualizar el seguimiento de ubicación según el nuevo estado
            UpdateLocationTracking();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error actualizando estado: {ex.Message}");
        }
    }

    public override void OnDestroy()
    {
        try
        {
            if (_isLocationUpdatesActive && Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                StopForeground(true); // Remueve la notificación
            }
            
            // Desregistrar instancia
            DeliveryTrackingServiceFixed.UnregisterServiceInstance();

            _locationManager?.RemoveUpdates(this);
            _isLocationUpdatesActive = false;
            _locationThread?.QuitSafely();
            _locationThread = null;
            _locationHandler = null;
            Console.WriteLine("🔴 Servicio de ubicación detenido");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error deteniendo servicio: {ex.Message}");
        }
        finally
        {
            base.OnDestroy();
        }
    }

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }
}