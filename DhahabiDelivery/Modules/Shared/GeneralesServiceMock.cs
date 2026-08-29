using DhahabiDelivery.Modules.Shared.Services;
using Mensajeria;

namespace DhahabiDelivery.Modules.Shared;

public class GeneralesServiceMock : IGeneralesService
{
    public async Task<TasaCambioResumen[]> ObtenerTasasDeCambio()
    {
        await Task.Delay(1000);
        TasaCambioResumen[] tasasDeCambio =
        [
            new("CUP", "Peso Cubano", "CU", 1, "CUP", "")
        ];
        return tasasDeCambio;
    }
}