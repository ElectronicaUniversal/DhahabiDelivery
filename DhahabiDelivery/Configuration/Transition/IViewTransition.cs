namespace DhahabiDelivery.Configuration.Transition;

/// <summary>
///     Un servicio que provee la característica de transiciones a blazor.
/// </summary>
public interface IViewTransition
{
    /// <summary>
    ///     Comienza el efecto de transición a la vista
    /// </summary>
    ValueTask BeginAsync();

    /// <summary>
    ///     Finaliza el efecto de transición.
    /// </summary>
    ValueTask EndAsync();
}