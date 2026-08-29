using System.Net;
using DhahabiDelivery.Modules.Auth.Models;
using Microsoft.AspNetCore.Components;

namespace DhahabiDelivery.Modules.Auth.Pages;

public partial class PaginaOlvidarContrasenia
{
    /// <summary>
    ///     Dirección de correo electrónico
    /// </summary>
    [Parameter]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    ///     Modelo que debe de llegar desde los parámetros de la url o
    ///     el navegador puede mostrarlo automáticamente.
    /// </summary>
    [SupplyParameterFromForm]
    private ModeloEmail? Model { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Model ??= new ModeloEmail();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Model == null) return;
        Model.Email = Email;
    }

    private static string IsValid(string? content)
    {
        return string.IsNullOrEmpty(content) ? "form-input empty-input" : "form-input full-input";
    }

    private async void OnValidSubmit()
    {
        if (AuthService == null || Model == null) return;
        var codigo = await AuthService.SolicitarCambioContrasenia(Model.Email);
        if (string.IsNullOrEmpty(codigo)) return;
        var encodedEmail = WebUtility.UrlEncode(Model.Email);
        Nm.NavigateTo(
            $"{Configuration.Pages.ConfirmarEmail}/{ConfirmarEmailReason.ChangePassword}?email={encodedEmail}");
    }
}