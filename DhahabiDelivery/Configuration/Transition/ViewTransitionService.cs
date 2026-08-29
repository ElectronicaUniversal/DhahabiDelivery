using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DhahabiDelivery.Configuration.Transition;

/// <summary>
///     Servicio que controla las transiciones de vista en Blazor.
/// </summary>
internal class ViewTransitionService(IJSRuntime jsRuntime) : IViewTransition, IAsyncDisposable
{
    private const string ScriptPath =
        "./Configuration/Transition/ViewTransitionRouter.razor.js";

    private IJSObjectReference? _module;

    private IJSObjectReference? _resolver;

    [Inject] private IJSRuntime JsRuntime { get; set; } = jsRuntime;

    /// <summary>
    ///     Libera los recursos utilizados por el servicio.
    /// </summary>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await InvokeResolver("reject");
            if (_module is not null) await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>
    ///     Inicia una transición de vista.
    /// </summary>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    public async ValueTask BeginAsync()
    {
        await InvokeResolver("reject");
        var module = await GetModuleAsync();
        _resolver = await module.InvokeAsync<IJSObjectReference>("beginViewTransition");
    }

    /// <summary>
    ///     Finaliza una transición de vista.
    /// </summary>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    public async ValueTask EndAsync()
    {
        await InvokeResolver("resolve");
    }

    /// <summary>
    ///     Obtiene el módulo JavaScript importado.
    /// </summary>
    /// <returns>Una tarea que representa la operación asincrónica y devuelve la referencia al módulo JavaScript.</returns>
    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", ScriptPath);
        return _module;
    }

    /// <summary>
    ///     Invoca un método en el objeto JavaScript almacenado en _Resolver.
    /// </summary>
    /// <param name="method">El nombre del método a invocar.</param>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    private async ValueTask InvokeResolver(string method)
    {
        var resolver = Interlocked.Exchange(ref _resolver, null);
        if (resolver == null) return;
        await resolver.InvokeVoidAsync(method);
        await resolver.DisposeAsync();
    }
}