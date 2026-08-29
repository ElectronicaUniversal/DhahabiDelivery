using Microsoft.AspNetCore.Components;
using Mensajeria;
using System.Threading;

namespace DhahabiDelivery.Modules.Shared.Components;

public partial class SelectorTasaCambio
{
    private bool isModalOpen = false;
    private bool isClosing = false;

    [Parameter] 
    public TasaCambioResumen[] Tasas { get; set; } = [];
    
    [Parameter] 
    public TasaCambioResumen? TasaSeleccionada { get; set; }
    
    [Parameter] 
    public string Placeholder { get; set; } = "Seleccione una moneda";
    
    [Parameter] 
    public EventCallback<TasaCambioResumen> OnTasaSeleccionada { get; set; }

    private void ToggleModal()
    {
        if (isModalOpen)
            StartClosingAnimation();
        else
            isModalOpen = true;
    }

    private void StartClosingAnimation()
    {
        isClosing = true;
        StateHasChanged(); // Esto es crucial para que se apliquen las clases CSS antes de la animación
        
        // Programar el cierre real después de la animación
        var timer = new Timer(_ =>
        {
            InvokeAsync(() =>
            {
                isModalOpen = false;
                isClosing = false;
                StateHasChanged();
            });
        }, null, 300, Timeout.Infinite); // 300ms es la duración de la animación
    }

    private void CloseModal()
    {
        StartClosingAnimation();
    }

    private async Task SelectTasa(TasaCambioResumen tasa)
    {
        TasaSeleccionada = tasa;
        StartClosingAnimation();
        await OnTasaSeleccionada.InvokeAsync(tasa);
    }
}
