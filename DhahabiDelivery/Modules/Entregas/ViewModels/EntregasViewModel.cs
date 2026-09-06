using System.Globalization;
using System.Net;
using DhahabiDelivery.Configuration;
using DhahabiDelivery.Modules.Auth.Exceptions;
using DhahabiDelivery.Modules.Auth.Services;
using DhahabiDelivery.Modules.Entregas.Exceptions;
using DhahabiDelivery.Modules.Entregas.Services;
using DhahabiDelivery.Modules.Shared.Components.Buttons;
using DhahabiDelivery.Modules.Shared.Services;
using DhahabiDelivery.Modules.Shared.Maps;
using Mensajeria;

namespace DhahabiDelivery.Modules.Entregas.ViewModels;

public partial class EntregasViewModel(
    IEntregasService entregasService,
    IRepartidorService repartidorService,
    IStorageService storageService,
    AuthService authService,
    LocationService locationService)
{
    [ObservableProperty] private LatLngLiteral _center = new();

    // Marcador de ubicación del repartidor
    [ObservableProperty] private LatLngLiteral? _deliveryLocation;

    [ObservableProperty] private MarkerData _deliveryMarketData = new(1);
    [ObservableProperty] private EntregaResumen[] _entregasAsignadas = [];
    [ObservableProperty] private EntregaResumen? _entregaSeleccionada;
    [ObservableProperty] private bool _loadingMap = true;
    [ObservableProperty] private LoadingButton.State _loadingState = LoadingButton.State.Loading;

    [ObservableProperty] private MapOptions _mapOptions = new()
    {
        Zoom = 14
    };

    [ObservableProperty] private LatLngLiteral _marker = new();
    [ObservableProperty] private MarkerData _markerData = new();
    [ObservableProperty] private string _state = authService.GetDeliveryStateAsync();
    [ObservableProperty] private VendedorEntregaResumen? _vendedorSeleccionado;

    // Inicializar el ViewModel y suscribirse a eventos
    public void Initialize()
    {
        // Suscribirse al evento de ubicación actualizada
        locationService.LocationUpdated += OnLocationUpdated;
    }

    // Método para manejar actualizaciones de ubicación
    private void OnLocationUpdated(object? sender, LocationUpdatedEventArgs e)
    {
        // Actualizar marcador del mapa con la nueva posición
        DeliveryLocation = new LatLngLiteral(e.Location.Latitude, e.Location.Longitude);
        DeliveryMarketData.UpdatePosition(DeliveryLocation);

        // Solo actualizar si estamos en modo entrega
        if (State != ConstantesEstadoRepartidor.ENTREGANDO) return;
        repartidorService.UpdateLocation(DeliveryLocation);
    }

    // Limpiar al destruir el ViewModel
    public void Cleanup()
    {
        // Desuscribirse del evento de ubicación
        locationService.LocationUpdated -= OnLocationUpdated;
    }

    public async Task ObtenerEntregasAsignadasAsync(CancellationToken cancellationToken = default)
    {
        LoadingState = LoadingButton.State.Loading;
        try
        {
            // Siempre intentar obtener datos del backend primero
            EntregasAsignadas = await entregasService.ObtenerEntregas(cancellationToken);

            // Si la llamada es exitosa, actualizar el storage con los datos más recientes
            var entregaActual = EntregasAsignadas.FirstOrDefault();
            if (entregaActual != null)
                storageService.SetAsync(ConstantesEstadoRepartidor.ORDER_ASIGNED_KEY, entregaActual);
            else
                // Si no hay entregas asignadas en el backend, limpiar el storage
                storageService.Remove(ConstantesEstadoRepartidor.ORDER_ASIGNED_KEY);

            var entregaResumen = EntregasAsignadas.FirstOrDefault();
            var coordenadas = entregaResumen?.Coordenadas.Split(",");
            if (coordenadas is null || coordenadas.Length <= 1)
            {
                LoadingState = LoadingButton.State.Success;
                return;
            }

            decimal.TryParse(coordenadas.FirstOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture,
                out var latitude);
            decimal.TryParse(coordenadas.LastOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture,
                out var longitude);
            Center = new LatLngLiteral(latitude, longitude);
            MarkerData.UpdatePosition(Center);
            LoadingState = LoadingButton.State.Success;
        }
        catch (LoginException)
        {
            LoadingState = LoadingButton.State.Error;
            authService.Logout();
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == HttpStatusCode.Forbidden)
            {
                authService.Logout();
                LoadingState = LoadingButton.State.Error;
                return;
            }

            // Error de conectividad - usar datos guardados como fallback
            UsarDatosGuardadosComoFallback();
        }
        catch
        {
            // Error general - usar datos guardados como fallback
            UsarDatosGuardadosComoFallback();
        }
    }

    private void UsarDatosGuardadosComoFallback()
    {
        var savedOrder = storageService.GetAsync<EntregaResumen>(ConstantesEstadoRepartidor.ORDER_ASIGNED_KEY);
        if (savedOrder != null)
        {
            EntregasAsignadas = [savedOrder];
            Console.WriteLine("⚠️ Usando datos guardados debido a problemas de conectividad");

            // Procesar coordenadas del fallback
            var coordenadas = savedOrder.Coordenadas.Split(",");
            if (coordenadas.Length > 1)
            {
                decimal.TryParse(coordenadas.FirstOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var latitude);
                decimal.TryParse(coordenadas.LastOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var longitude);
                Center = new LatLngLiteral(latitude, longitude);
                MarkerData.UpdatePosition(Center);
            }

            LoadingState = LoadingButton.State.Success;
        }
        else
        {
            EntregasAsignadas = [];
            LoadingState = LoadingButton.State.Error;
            Console.WriteLine("❌ Sin conectividad y sin datos guardados");
        }
    }

    public async Task<string> ObtenerEstadoRepartidorAsync(
        CancellationToken cancellationToken = default)
    {
        var estado = await repartidorService.ObtenerEstadoRepartidor();
        // No forzar disponibilidad si ya hay una entrega en camino: EstablecerEstadoRepartidor(DISPONIBLE)
        // sin IdOrden usa la búsqueda legada por "Procesando", que con múltiples órdenes activas es
        // ambigua y el backend la interpreta como "el repartidor terminó la entrega", completando
        // silenciosamente una orden al azar (ver docs/superpowers/plans/2026-09-05-multi-orden-estado-entrega.md).
        if (estado is ConstantesEstadoRepartidor.ASIGNADO or ConstantesEstadoRepartidor.DISPONIBLE)
            await CambiarDisponibilidadAsync(true);
        return estado;
    }

    public async Task IniciarEntrega()
    {
        if (EntregaSeleccionada == null) return;

        // Verificar que el GPS esté activado
        var isGpsEnabled = await locationService.EnsureGpsEnabledAsync();
        if (!isGpsEnabled)
            // En lugar de lanzar una excepción genérica, usamos nuestra excepción específica
            throw new GpsNotEnabledException("El GPS debe estar activado para iniciar una entrega. " +
                                             "Por favor activa el GPS desde la configuración del dispositivo y vuelve a intentarlo.");

        var idOrdenIniciada = EntregaSeleccionada.Id;
        var state = await repartidorService.IniciarEntrega(EntregaSeleccionada);
        storageService.SetAsync(ConstantesEstadoRepartidor.ORDER_ASIGNED_KEY, EntregaSeleccionada);
        authService.SetDeliveryStateAsync(state);
        State = state;

        // Refrescar la lista para que EntregaSeleccionada.EstadoEnvio refleje "En camino"
        // (el objeto es inmutable, así que hay que volver a pedirlo en vez de mutarlo).
        await ObtenerEntregasAsignadasAsync();
        EntregaSeleccionada = EntregasAsignadas.FirstOrDefault(e => e.Id == idOrdenIniciada) ?? EntregaSeleccionada;

        // Actualizar el estado en el servicio de ubicación
        await locationService.UpdateDeliveryStateAsync(state);
        Console.WriteLine($"🚚 Entrega iniciada - Estado: {state}");
    }

    public async Task FinalizarEntrega()
    {
        // Obtener el estado actual del repartidor
        var estadoActual = await repartidorService.ObtenerEstadoRepartidor();

        // Verificar si el repartidor está en estado "E"
        if (estadoActual != ConstantesEstadoRepartidor.ENTREGANDO &&
            estadoActual != ConstantesEstadoRepartidor.DISPONIBLE)
            // No permitir finalizar la entrega si no está en estado "E"
            return;

        if (EntregaSeleccionada == null) return;

        var state = await repartidorService.FinalizarEntrega(EntregaSeleccionada.Id);
        EntregaSeleccionada = null;
        EntregasAsignadas = [];
        storageService.Remove(ConstantesEstadoRepartidor.ORDER_ASIGNED_KEY);
        authService.SetDeliveryStateAsync(state);
        State = state;

        // Actualizar el estado en el servicio de ubicación
        await locationService.UpdateDeliveryStateAsync(state);
        Console.WriteLine($"✅ Entrega finalizada - Estado: {state}");
    }

    // Verdadero si alguna otra entrega asignada (distinta a la que se le pasa) ya está en camino.
    public bool TieneOtraEntregaEnCamino(EntregaResumen entrega) =>
        EntregasAsignadas.Any(e => e.Id != entrega.Id && e.EstadoEnvio == ConstantesEstadoEnvio.ENCAMINO);

    public async Task CambiarDisponibilidadAsync(bool disponible)
    {
        try
        {
            // Verificar permisos de ubicación antes de cambiar a disponible
            if (disponible)
            {
                // Verificar permisos de ubicación
                var permissionStatus = await LocationService.CheckAndRequestLocationPermissionAsync();
                if (permissionStatus != PermissionStatus.Granted)
                    throw new InvalidOperationException("Se requieren permisos de ubicación para estar disponible");

                // Verificar si el GPS está activado
                var isGpsEnabled = await locationService.EnsureGpsEnabledAsync();
                if (!isGpsEnabled)
                    // En lugar de lanzar una excepción, retornamos y notificamos al usuario
                    // El diálogo lo manejará el componente de UI
                    throw new GpsNotEnabledException("Es necesario activar el GPS para estar disponible. " +
                                                     "Por favor activa el GPS desde la configuración del dispositivo y vuelve a intentarlo.");
            }

            // Cambiar estado del repartidor
            var newState =
                disponible ? ConstantesEstadoRepartidor.DISPONIBLE : ConstantesEstadoRepartidor.NO_DISPONIBLE;
            var state = await repartidorService.EstablecerEstadoRepartidor(newState);

            // Actualizar estado local
            authService.SetDeliveryStateAsync(state);
            State = state;

            // Actualizar el estado en el servicio de ubicación
            await locationService.UpdateDeliveryStateAsync(state);

            Console.WriteLine(
                $"🔄 Estado de disponibilidad cambiado a: {(disponible ? "Disponible" : "No Disponible")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al cambiar disponibilidad: {ex.Message}");
            throw;
        }
    }
}