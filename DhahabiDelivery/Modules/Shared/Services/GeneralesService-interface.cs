using Mensajeria;

namespace DhahabiDelivery.Modules.Shared.Services;

public interface IGeneralesService
{
    Task<TasaCambioResumen[]> ObtenerTasasDeCambio();
}