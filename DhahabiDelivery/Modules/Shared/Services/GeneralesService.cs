using DhahabiDelivery.Configuration;
using Mensajeria;

namespace DhahabiDelivery.Modules.Shared.Services;

public class GeneralesService(HttpHelper httpHelper) : IGeneralesService
{
    public async Task<TasaCambioResumen[]> ObtenerTasasDeCambio()
    {
        var req = new ObtenerTasaCambioResumenListaRequest();
        var config = new HttpHelper.HttpHelperConfig(Apis.GeneralesQuery.Name,
            Apis.GeneralesQuery.ObtenerTasaCambioResumenLista);
        var res = await httpHelper.HttpRequestAsync(req, config);

        return res.TasasCambio;
    }
}