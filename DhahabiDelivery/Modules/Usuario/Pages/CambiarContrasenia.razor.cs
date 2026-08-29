using System.Net;
using DhahabiDelivery.Configuration;
using DhahabiDelivery.Modules.Shared.Components.Buttons;
using DhahabiDelivery.Modules.Shared.Components.Dialogs;
using DhahabiDelivery.Modules.Shared.Services;
using DhahabiDelivery.Modules.Usuario.Models;
using DhahabiDelivery.Shared.Resources;
using Mensajeria;
using Microsoft.AspNetCore.Components;

namespace DhahabiDelivery.Modules.Usuario.Pages;

public partial class CambiarContrasenia
{
    private LoadingButton.State _buttonState = LoadingButton.State.Normal;
    private Dialog _dialog = null!;
    private string _dialogBody = string.Empty;
    private string _email = string.Empty;
    [SupplyParameterFromQuery] public string Email { get; set; } = string.Empty;
    [SupplyParameterFromForm] public EmailModel? Model { get; set; }

    [Inject] public required HttpHelper Helper { get; set; }

    protected override void OnInitialized()
    {
        Model ??= new EmailModel();
    }

    protected override void OnParametersSet()
    {
        _email = WebUtility.UrlDecode(Email);
    }

    private async void OnValidSubmit()
    {
        _buttonState = LoadingButton.State.Loading;
        StateHasChanged();
        try
        {
            var req = new CambiarContrasenaCuentaRequest(_email, Model!.Password);
            var config = new HttpHelper.HttpHelperConfig
            {
                Api = Apis.AuthenticationCommand.Name,
                Url = Apis.AuthenticationCommand.CambiarContrasenaCliente
            };
            var res =
                await Helper.MakeHttpRequestAsync<CambiarContrasenaCuentaRequest, CambiarContrasenaCuentaResponse>(req,
                    config);
            if (res.Codigo.Equals(string.Empty)) throw new Exception();
            Nm.NavigateTo(Configuration.Pages.Usuario);
            return;
        }
        catch (HttpRequestException)
        {
            _dialogBody = Language[Idioma.ErrorDeConexion];
        }
        catch (Exception)
        {
            _dialogBody = Language[Idioma.ErrorDesconocido];
        }

        await _dialog.Open();
        _buttonState = LoadingButton.State.Error;
        StateHasChanged();
    }
}