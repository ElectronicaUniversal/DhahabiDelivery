namespace DhahabiDelivery.Configuration.Transition;

/// <summary>
///     Métodos de extensión para agregar servicios de transición de vistas en Blazor.
/// </summary>
public static class ViewTransitionServiceExtension
{
    /// <summary>
    ///     Agrega un servicio de transición de vistas de Blazor al Microsoft. Extensions.
    ///     DependencyInjection. IServiceCollection
    ///     especificado.
    /// </summary>
    /// <param name="services">
    ///     El Microsoft. Extensions. DependencyInjection. IServiceCollection al que se agregará el
    ///     servicio.
    /// </param>
    public static void AddViewTransition(this IServiceCollection services)
    {
        services.AddScoped<IViewTransition, ViewTransitionService>();
    }
}