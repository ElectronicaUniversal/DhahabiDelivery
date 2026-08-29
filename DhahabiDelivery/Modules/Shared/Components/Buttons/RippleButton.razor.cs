using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DhahabiDelivery.Modules.Shared.Components.Buttons;

public partial class RippleButton : ComponentBase, IAsyncDisposable
{
    private ElementReference _button;
    private DotNetObjectReference<RippleButton>? _currentComponentReference;

    private IJSObjectReference? _module;

    [Inject] private IJSRuntime? JsRuntime { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public string Style { get; set; } = string.Empty;

    [Parameter] public EventCallback OnClick { get; set; }

    [Parameter]
    public string Background { get; set; } = "linear-gradient(to right, var(--primary-color), var(--secondary-color))";

    [Parameter] public string Estilo { get; set; } = "normal";

    [Parameter] public string RippleColor { get; set; } = "white";

    /// <summary>
    ///     Método de eliminación asincrónica del componente.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_module != null) await _module.DisposeAsync();
        _currentComponentReference?.Dispose();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || JsRuntime == null) return;
        _currentComponentReference = DotNetObjectReference.Create(this);
        _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import",
            "./RippleButton.js");
        await _module.InvokeVoidAsync("createRipple", _button, _currentComponentReference, RippleColor);
    }

    [JSInvokable]
    public async Task OnTouch()
    {
        await OnClick.InvokeAsync(this);
    }
}