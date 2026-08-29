using DhahabiDelivery.Configuration;
using DhahabiDelivery.Modules.Shared.Services;
using Mensajeria;

namespace DhahabiDelivery.Modules.Scanner;

public partial class BarCodeViewModel(HttpHelper httpHelper)
{
    [ObservableProperty] private string _error;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObtenerOrdenCarritoClienteResponse _order;
    [ObservableProperty] private string _scanResultValue;
}