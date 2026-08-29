using DhahabiDelivery.Configuration;
using DhahabiDelivery.Modules.Shared.Services;
using TasaCambio = Mensajeria.TasaCambioResumen;

namespace DhahabiDelivery.Modules.Layout;

public partial class MainViewModel(IGeneralesService generalesService, IStorageService storageService)
{
    [ObservableProperty] private bool _cargandoTasasDeCambio;
    [ObservableProperty] private bool _errorTasasDeCambio;
    private string? _savedTasaCambioId;
    [ObservableProperty] private TasaCambio[] _tasaCambioResumen = [];
    [ObservableProperty] private TasaCambio? _tasaCambioResumenSeleccionada;

    public void InitializeAsync()
    {
        _savedTasaCambioId = storageService.GetAsync<string>("tasaCambioSeleccionada");
    }

    public void SeleccionarTasaDeCambio(TasaCambio tasaCambio)
    {
        TasaCambioResumenSeleccionada = tasaCambio;
        storageService.SetAsync("tasaCambioSeleccionada", tasaCambio.Codigo);
    }

    public async Task ObtenerTasasDeCambio()
    {
        ErrorTasasDeCambio = false;
        CargandoTasasDeCambio = true;

        try
        {
            var tasasDeCambio = await generalesService.ObtenerTasasDeCambio();
            TasaCambioResumen = tasasDeCambio;

            // Set selection from saved ID
            if (!string.IsNullOrEmpty(_savedTasaCambioId))
                TasaCambioResumenSeleccionada = TasaCambioResumen.FirstOrDefault(t => t.Codigo == _savedTasaCambioId);
        }
        catch
        {
            ErrorTasasDeCambio = true;
        }
        finally
        {
            CargandoTasasDeCambio = false;
        }
    }
}