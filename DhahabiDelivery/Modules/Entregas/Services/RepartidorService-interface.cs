using GoogleMapsComponents.Maps;
using Mensajeria;

namespace DhahabiDelivery.Modules.Entregas.Services;

public interface IRepartidorService
{
    Task<string> IniciarEntrega(EntregaResumen ordenAsignada);
    Task<string> FinalizarEntrega();
    Task<string> ObtenerEstadoRepartidor();
    Task<string> EstablecerEstadoRepartidor(string estado);
    Task UpdateLocation(LatLngLiteral deliveryLocation);
}