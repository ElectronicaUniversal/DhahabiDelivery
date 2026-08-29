using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace DhahabiDelivery.Configuration.Transition;

public partial class ViewTransitionRouter
{
    private readonly string[] _bloquedUrls = ["", "productos"];

    private bool _firstRendered;

    private Dictionary<string, object> _parameters = [];

    private IViewTransition? _viewTransition;

    [Inject] private IJSRuntime? JsRuntime { get; set; }

    /// <summary>
    ///     Obtiene o establece el tipo de componente router a utilizar. El valor predeterminado es el tipo de
    ///     <see cref="Router" />.
    /// </summary>
    [Parameter]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                                DynamicallyAccessedMemberTypes.PublicProperties |
                                DynamicallyAccessedMemberTypes.NonPublicProperties)]
    public Type TypeOfRouter { get; set; } = typeof(Router);

    /// <summary>
    ///     Obtiene o establece la biblioteca que debe ser buscada para componentes que coincidan con la URI.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public Assembly AppAssembly { get; set; } = null!;

    /// <summary>
    ///     Obtiene o establece una coleccion de bibliotecas adicionales que deben ser buscadas para componentes
    ///     que puedan coincidir con URI.
    /// </summary>
    [Parameter]
    public IEnumerable<Assembly> AdditionalAssemblies { get; set; } = [];

    /// <summary>
    ///     Obtiene o establece el contenido a mostrar cuando no se encuentra una coincidencia para la ruta solicitada.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment NotFound { get; set; } = null!;

    /// <summary>
    ///     Obtiene o establece el contenido a mostrar cuando se encuentra una coincidencia para la ruta solicitada.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment<RouteData> Found { get; set; } = null!;

    /// <summary>
    ///     Obtiene o establece el contenido a mostrar cuando la navegacion asincronica está en progreso.
    /// </summary>
    [Parameter]
    public RenderFragment? Navigating { get; set; }

    /// <summary>
    ///     Obtiene o establece un controlador que debe ser llamado antes de navegar a una nueva página.
    /// </summary>
    [Parameter]
    public EventCallback<NavigationContext> OnNavigateAsync { get; set; }

    /// <summary>
    ///     Obtiene o establece una bandera que indica si la coincidencia de rutas debe preferir coincidencias exactas
    ///     sobre comodines.
    ///     <para>Esta propiedad est� obsoleta y configurarla no tiene ningun efecto.</para>
    /// </summary>
    [Parameter]
    public bool PreferExactMatches { get; set; }

    /// <summary>
    ///     Libera los recursos utilizados por el componente de forma asincronica.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_viewTransition is IAsyncDisposable viewTransition) await viewTransition.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>
    ///     Obtiene el servicio de transicion de vista.
    /// </summary>
    private IViewTransition? GetViewTransition()
    {
        _viewTransition ??= ServiceProvider.GetService<IViewTransition>();
        if (JsRuntime != null) _viewTransition ??= new ViewTransitionService(JsRuntime);
        return _viewTransition;
    }


    protected override void OnParametersSet()
    {
        _parameters = new Dictionary<string, object>
        {
            ["AppAssembly"] = AppAssembly,
            ["AdditionalAssemblies"] = AdditionalAssemblies,
            ["NotFound"] = NotFound,
            ["Found"] = Found,
            ["Navigating"] = Navigating!,
            ["OnNavigateAsync"] = EventCallback.Factory.Create<NavigationContext>(this, OnNavigateAsyncInternal),
            ["PreferExactMatches"] = PreferExactMatches
        };
    }

    /// <summary>
    ///     Metodo interno que se invoca cuando se produce una navegacion asincronica.
    /// </summary>
    /// <param name="navigationContext">El contexto de la navegacion.</param>
    private async Task OnNavigateAsyncInternal(NavigationContext navigationContext)
    {
        if (!_bloquedUrls.Contains(navigationContext.Path) && _firstRendered)
        {
            var transition = GetViewTransition();
            if (transition != null) await transition.BeginAsync();
        }

        await OnNavigateAsync.InvokeAsync(navigationContext);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _firstRendered = true;
        }
        else
        {
            var transition = GetViewTransition();
            if (transition != null) await transition.EndAsync();
        }
    }
}