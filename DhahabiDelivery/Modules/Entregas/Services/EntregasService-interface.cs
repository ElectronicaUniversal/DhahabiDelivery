using Mensajeria;

namespace DhahabiDelivery.Modules.Entregas.Services;

public interface IEntregasService
{
    Task<EntregaResumen[]> ObtenerEntregas(CancellationToken cancellationToken);
}