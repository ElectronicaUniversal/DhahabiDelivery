using System.ComponentModel;
using System.Runtime.CompilerServices;
using DhahabiDelivery.Modules.Entregas;

namespace DhahabiDelivery.Modules.Shared.Services;

/// <summary>
///     Servicio centralizado para gestionar ubicación y estados del repartidor.
///     Implementado como singleton para ser inyectado en ViewModels y accedido desde servicios nativos.
/// </summary>
public sealed class LocationService : INotifyPropertyChanged
{
    private string _currentDeliveryState = ConstantesEstadoRepartidor.NO_DISPONIBLE;
    private bool _isGpsEnabled;
    private bool _isLocationServiceActive;
    private Location? _lastKnownLocation;

    /// <summary>
    ///     Estado actual del repartidor (NO_DISPONIBLE, DISPONIBLE, ENTREGANDO, etc)
    /// </summary>
    public string CurrentDeliveryState
    {
        get => _currentDeliveryState;
        set
        {
            Console.WriteLine(
                $"[CurrentDeliveryState_setter] Actualizando estado del repartidor a: {value} (old value) {_currentDeliveryState}");
            _currentDeliveryState = value;
            OnPropertyChanged();

            // Notificar a los servicios nativos del cambio de estado
            UpdateAndroidServiceState();
        }
    }

    /// <summary>
    ///     Indica si el servicio de ubicación está activo
    /// </summary>
    public bool IsLocationServiceActive
    {
        get => _isLocationServiceActive;
        private set
        {
            if (_isLocationServiceActive == value) return;
            _isLocationServiceActive = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     Indica si el GPS del dispositivo está habilitado
    /// </summary>
    public bool IsGpsEnabled
    {
        get => _isGpsEnabled;
        private set
        {
            if (_isGpsEnabled == value) return;
            _isGpsEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     Última ubicación conocida del dispositivo
    /// </summary>
    public Location? LastKnownLocation
    {
        get => _lastKnownLocation;
        private set
        {
            _lastKnownLocation = value;
            OnPropertyChanged();
        }
    }

    // Evento para notificar cambios en propiedades
    public event PropertyChangedEventHandler? PropertyChanged;

    // Evento para notificar actualizaciones de ubicación
    public event EventHandler<LocationUpdatedEventArgs>? LocationUpdated;

    /// <summary>
    ///     Actualiza el estado del repartidor y gestiona el servicio de ubicación
    /// </summary>
    public async Task UpdateDeliveryStateAsync(string newState)
    {
        Console.WriteLine($"[UpdateDeliveryStateAsync] Actualizando estado del repartidor a: {newState}");
        CurrentDeliveryState = newState;

        // Verificar si necesitamos activar la ubicación basado en el nuevo estado
        if (newState is ConstantesEstadoRepartidor.DISPONIBLE or ConstantesEstadoRepartidor.ENTREGANDO)
        {
            // Verificar y solicitar permiso de ubicación si es necesario
            var permissionStatus = await CheckAndRequestLocationPermissionAsync();
            if (permissionStatus != PermissionStatus.Granted)
                throw new InvalidOperationException("Se requieren permisos de ubicación para activar este estado");

            // Verificar si el GPS está habilitado
            if (!await EnsureGpsEnabledAsync())
                throw new InvalidOperationException("El GPS debe estar activado para utilizar esta funcionalidad");
        }
    }

    /// <summary>
    ///     Verifica y solicita permisos de ubicación si es necesario
    /// </summary>
    public static async Task<PermissionStatus> CheckAndRequestLocationPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        return status;
    }

    /// <summary>
    ///     Actualiza la ubicación desde los servicios nativos
    /// </summary>
    public void UpdateLocation(double latitude, double longitude, double accuracy, string provider)
    {
        var newLocation = new Location(latitude, longitude)
        {
            Accuracy = accuracy,
            Timestamp = DateTimeOffset.UtcNow,
            AltitudeReferenceSystem = AltitudeReferenceSystem.Unspecified
        };

        LastKnownLocation = newLocation;

        // Notificar a los suscriptores de la actualización
        LocationUpdated?.Invoke(this, new LocationUpdatedEventArgs(newLocation, provider));

        // Log de información de ubicación
        Console.WriteLine(
            $"📍 Ubicación actualizada: {latitude:F6}, {longitude:F6} | Precisión: {accuracy}m | Proveedor: {provider}");
    }

    /// <summary>
    ///     Verifica si el GPS está habilitado y solicita al usuario activarlo si no lo está
    /// </summary>
    public async Task<bool> EnsureGpsEnabledAsync()
    {
#if ANDROID
        var isEnabled = await GpsUtils.IsGpsEnabledAsync();
        IsGpsEnabled = isEnabled;
        return isEnabled;
#endif
        return false;
    }

    /// <summary>
    ///     Actualiza el estado del servicio nativo de Android
    /// </summary>
    private void UpdateAndroidServiceState()
    {
        Console.WriteLine("[UpdateAndroidServiceState 158] Actualizando estado del servicio de Android");
#if ANDROID
        switch (CurrentDeliveryState)
        {
            case ConstantesEstadoRepartidor.DISPONIBLE:
            case ConstantesEstadoRepartidor.ENTREGANDO:
                // Verificar primero si el servicio ya está en ejecución
                if (!DeliveryTrackingServiceFixed.CheckIfServiceIsRunning())
                {
                    DeliveryTrackingServiceFixed.UpdateDeliveryState(CurrentDeliveryState);
                    DeliveryTrackingServiceFixed.Start();
                }
                else
                {
                    // Solo actualizar el estado si el servicio ya está en ejecución
                    DeliveryTrackingServiceFixed.UpdateDeliveryState(CurrentDeliveryState);
                }

                IsLocationServiceActive = true;
                break;

            default:
                // Detener servicio
                DeliveryTrackingServiceFixed.UpdateDeliveryState(CurrentDeliveryState);
                DeliveryTrackingServiceFixed.Stop();
                IsLocationServiceActive = false;
                break;
        }
#endif
    }

    // Notificador de cambios de propiedad
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
///     Argumentos para el evento de actualización de ubicación
/// </summary>
public class LocationUpdatedEventArgs : EventArgs
{
    public LocationUpdatedEventArgs(Location location, string provider)
    {
        Location = location;
        Provider = provider;
    }

    public Location Location { get; }
    public string Provider { get; }
}