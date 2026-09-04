using System.Globalization;
using DhahabiDelivery.Configuration;
using DhahabiDelivery.Modules.Shared.Services;
using DhahabiDelivery.Modules.Shared.Maps;
using Mensajeria;

namespace DhahabiDelivery.Modules.Entregas.Services;

public class RepartidorServiceMock(
    HttpHelper httpHelper,
    IStorageService storageService) : IRepartidorService
{
    private string _estado = ConstantesEstadoRepartidor.ASIGNADO;

    public async Task<string> IniciarEntrega(EntregaResumen ordenAsignada)
    {
        await Task.Delay(1000);
        _estado = ConstantesEstadoRepartidor.ENTREGANDO;
        return ConstantesEstadoRepartidor.ENTREGANDO;
    }

    public async Task<string> FinalizarEntrega()
    {
        await Task.Delay(1000);
        _estado = ConstantesEstadoRepartidor.DISPONIBLE;
        return ConstantesEstadoRepartidor.DISPONIBLE;
    }

    public async Task<string> ObtenerEstadoRepartidor()
    {
        await Task.Delay(1000);
        return _estado;
    }

    public async Task<string> EstablecerEstadoRepartidor(string estado)
    {
        await Task.Delay(1000);
        _estado = estado;
        return estado;
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