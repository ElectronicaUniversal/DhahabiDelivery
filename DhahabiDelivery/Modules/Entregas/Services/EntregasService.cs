using DhahabiDelivery.Configuration;
using DhahabiDelivery.Modules.Shared.Services;
using Mensajeria;

namespace DhahabiDelivery.Modules.Entregas.Services;

public class EntregasService(HttpHelper httpHelper) : IEntregasService
{
    public async Task<EntregaResumen[]> ObtenerEntregas(CancellationToken cancellationToken)
    {
        var req = new ObtenerEntregasResumenListaRequest();
        var config =
            new HttpHelper.HttpHelperConfig(Apis.VentasQuery.Name, Apis.VentasQuery.ObtenerEntregasResumenLista);

        var res = await httpHelper
            .MakeHttpRequestAsync<ObtenerEntregasResumenListaRequest, ObtenerEntregasResumenListaResponse>(req, config,
                cancellationToken);
        return res.Entregas;
    }
}