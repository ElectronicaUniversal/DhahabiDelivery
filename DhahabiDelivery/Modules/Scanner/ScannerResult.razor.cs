using DhahabiDelivery.Configuration;
using DhahabiDelivery.Modules.Entregas;
using DhahabiDelivery.Modules.Shared.Components.Buttons;
using Mensajeria;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Timer = System.Timers.Timer;

namespace DhahabiDelivery.Modules.Scanner;

public partial class ScannerResult
{
    private string _errorMessage = string.Empty;
    private bool _isMatch;
    private bool _loading = true;

    private LoadingButton.State _loadingCompletarOrden = LoadingButton.State.Normal;
    private EntregaResumen? _matchedOrder;
    private bool _processingComplete;
    private bool _showErrorMessage;
    private Timer? _timer;
    [Parameter] [SupplyParameterFromQuery] public string OrderId { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await base.OnInitializedAsync();

            _timer = new Timer(800);

            // Simular carga inicial y verificación con tiempo de espera seguro
            _timer.Elapsed += async (sender, args) =>
            {
                try
                {
                    await InvokeAsync(() =>
                    {
                        try
                        {
                            _loading = false;
                            VerificarOrden();
                            StateHasChanged();
                        }
                        catch (Exception ex)
                        {
                            // Si hay un error en la verificación, mostrar una UI de error genérica
                            _loading = false;
                            _isMatch = false;
                            Console.WriteLine($"Error al verificar orden: {ex.Message}");
                            StateHasChanged();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en el timer: {ex.Message}");
                }
                finally
                {
                    // Asegurar que el timer siempre se limpie
                    _timer?.Stop();
                    _timer?.Dispose();
                }
            };
            _timer.AutoReset = false;
            _timer.Start();
        }
        catch (Exception ex)
        {
            // Manejar cualquier error durante la inicialización
            _loading = false;
            _isMatch = false;
            Console.WriteLine($"Error al inicializar: {ex.Message}");
            StateHasChanged();
        }
    }

    private void VerificarOrden()
    {
        try
        {
            // Resetear estado para evitar problemas
            _isMatch = false;
            _matchedOrder = null;

            if (string.IsNullOrWhiteSpace(OrderId))
            {
                Console.WriteLine("OrderId está vacío");
                return;
            }

            // Validar que ViewModel no sea nulo y que tenga EntregasAsignadas

            if (ViewModel.EntregasAsignadas == null)
            {
                Console.WriteLine("EntregasAsignadas es nulo");
                return;
            }

            try
            {
                // Asegurarnos que la comparación se hace como string siempre
                var orderIdStr = OrderId.Trim();

                // Verificar que tengamos entregas asignadas para comparar
                if (ViewModel.EntregasAsignadas.Length == 0)
                {
                    Console.WriteLine("No hay entregas asignadas para comparar");
                    return;
                }

                // Buscar si el OrderId coincide con alguna entrega asignada
                // Uso de operador de navegación segura para evitar NullReferenceException
                _matchedOrder = ViewModel.EntregasAsignadas
                    .FirstOrDefault(e => e != null && e.Id.ToString() == orderIdStr);

                _isMatch = _matchedOrder != null;

                // Log para debug
                if (_isMatch)
                {
                    Console.WriteLine($"Orden encontrada: {_matchedOrder.Id}");
                }
                else
                {
                    Console.WriteLine($"Orden no encontrada: {orderIdStr}");
                    if (ViewModel.EntregasAsignadas.Length > 0)
                        Console.WriteLine(
                            $"Órdenes disponibles: {string.Join(", ", ViewModel.EntregasAsignadas.Select(e => e?.Id))}");
                    else
                        Console.WriteLine("No hay órdenes asignadas");
                }
            }
            catch (Exception ex)
            {
                // Manejar específicamente errores de conversión o comparación
                Console.WriteLine($"Error al comparar OrderId: {ex.Message}");
                _isMatch = false;
            }

            // Aplicar efectos de sonido según el resultado
            try
            {
                if (_isMatch)
                    PlaySuccessSound();
                else
                    PlayErrorSound();
            }
            catch (Exception ex)
            {
                // No dejar que un error en los sonidos rompa la funcionalidad principal
                Console.WriteLine($"Error al reproducir sonido: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            // Manejar cualquier otro error durante la verificación
            _isMatch = false;
            _matchedOrder = null;
            Console.WriteLine($"Error general en VerificarOrden: {ex.Message}");
        }
    }

    private async Task CompletarOrden()
    {
        try
        {
            _loadingCompletarOrden = LoadingButton.State.Loading;
            StateHasChanged();

            if (_matchedOrder == null)
            {
                MostrarError("No se pudo completar la entrega. Datos incompletos.");
                return;
            }

            if (ViewModel.State != ConstantesEstadoRepartidor.ENTREGANDO)
            {
                // Mostrar mensaje de error visual si el repartidor no está en estado "Entregando"
                MostrarError("No puedes completar esta entrega porque aún no la has iniciado");
                return;
            }

            ViewModel.EntregaSeleccionada = _matchedOrder;
            await ViewModel.FinalizarEntrega();
            _processingComplete = true;
            _loadingCompletarOrden = LoadingButton.State.Success;
            StateHasChanged();
            try
            {
                PlaySuccessSound();
            }
            catch (Exception soundEx)
            {
                Console.WriteLine($"Error al reproducir sonido: {soundEx.Message}");
                // No permitir que un error de sonido interrumpa la funcionalidad principal
            }
        }
        catch (Exception ex)
        {
            _loadingCompletarOrden = LoadingButton.State.Error;
            // Manejar error al completar la entrega
            Console.WriteLine($"Error al completar orden: {ex.Message}");
            MostrarError($"Error al completar la entrega: {ex.Message}");
            StateHasChanged();
        }
    }

    private void VerDetalles()
    {
        try
        {
            if (_matchedOrder != null)
                NavigationManager.NavigateTo($"{Pages.EntregasDetail}/{_matchedOrder.Id}");
            else
                // Protección adicional por si de alguna manera el botón está visible cuando no debería
                Console.WriteLine("No se puede navegar a los detalles: no hay orden seleccionada");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al navegar a detalles: {ex.Message}");
        }
    }

    private void EscanearNuevamente()
    {
        try
        {
            NavigationManager.NavigateTo("/scanner");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al navegar a scanner: {ex.Message}");
        }
    }

    private void VolverAlInicio()
    {
        try
        {
            NavigationManager.NavigateTo("/");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al navegar al inicio: {ex.Message}");
        }
    }

    private string GetAnimationClass()
    {
        if (_loading) return "animate-pulse";
        return _isMatch ? "animate-bounce" : "animate-shake";
    }

    private string GetContentClass()
    {
        return _loading ? "opacity-50" : "opacity-100";
    }

    private async void PlaySuccessSound()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("playSound", "success");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al reproducir sonido de éxito: {ex.Message}");
            // Fallar silenciosamente - no queremos que un problema de sonido interrumpa la UX
        }
    }

    private async void PlayErrorSound()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("playSound", "error");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al reproducir sonido de error: {ex.Message}");
            // Fallar silenciosamente - no queremos que un problema de sonido interrumpa la UX
        }
    }

    private void MostrarError(string mensaje)
    {
        _errorMessage = mensaje;
        _showErrorMessage = true;
    }
}