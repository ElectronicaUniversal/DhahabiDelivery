using System.Globalization;
using DhahabiDelivery.Configuration;
using DhahabiDelivery.Modules.Shared.Services;
using GoogleMapsComponents.Maps;
using Mensajeria;

namespace DhahabiDelivery.Modules.Entregas.Services;

public class RepartidorService(
    HttpHelper httpHelper,
    IStorageService storageService) : IRepartidorService
{
    public async Task<string> IniciarEntrega(EntregaResumen ordenAsignada)
    {
        var req = new EstablecerEstadoRepartidorRequest(ConstantesEstadoRepartidor.ENTREGANDO);
        var config =
            new HttpHelper.HttpHelperConfig(Apis.AgentesCommand.Name, Apis.AgentesCommand.EstablecerEstadoRepartidor);
        var res = await httpHelper
            .MakeHttpRequestAsync<EstablecerEstadoRepartidorRequest, EstablecerEstadoRepartidorResponse>(req, config);
        return res.EstadoRepartidor;
    }

    public async Task<string> FinalizarEntrega()
    {
        var req = new EstablecerEstadoRepartidorRequest(ConstantesEstadoRepartidor.DISPONIBLE);
        var config =
            new HttpHelper.HttpHelperConfig(Apis.AgentesCommand.Name, Apis.AgentesCommand.EstablecerEstadoRepartidor);
        var res = await httpHelper
            .MakeHttpRequestAsync<EstablecerEstadoRepartidorRequest, EstablecerEstadoRepartidorResponse>(req, config);
        return res.EstadoRepartidor;
    }

    public async Task<string> ObtenerEstadoRepartidor()
    {
        var req = new ObtenerEstadoRepartidorRequest();
        var config = new HttpHelper.HttpHelperConfig(Apis.AgentesQuery.Name, Apis.AgentesQuery.ObtenerEstadoRepartidor);
        var res =
            await httpHelper.MakeHttpRequestAsync<ObtenerEstadoRepartidorRequest, ObtenerEstadoRepartidorResponse>(req,
                config);
        return res.Estado;
    }

    public async Task<string> EstablecerEstadoRepartidor(string estado)
    {
        var req = new EstablecerEstadoRepartidorRequest(estado);
        var config =
            new HttpHelper.HttpHelperConfig(Apis.AgentesCommand.Name, Apis.AgentesCommand.EstablecerEstadoRepartidor);
        var res = await httpHelper
            .MakeHttpRequestAsync<EstablecerEstadoRepartidorRequest, EstablecerEstadoRepartidorResponse>(req, config);
        return res.EstadoRepartidor;
    }

    public async Task UpdateLocation(LatLngLiteral deliveryLocation)
    {
        var req = new ActualizarPosicionRepartidorRequest(
            deliveryLocation.Lat.ToString(CultureInfo.InvariantCulture),
            deliveryLocation.Lng.ToString(CultureInfo.InvariantCulture)
        );
        var config =
            new HttpHelper.HttpHelperConfig(Apis.AgentesCommand.Name, Apis.AgentesCommand.ActualizarPosicionRepartidor);
        await httpHelper
            .MakeHttpRequestAsync<ActualizarPosicionRepartidorRequest, ActualizarPosicionRepartidorResponse>(req,
                config);
    }
}