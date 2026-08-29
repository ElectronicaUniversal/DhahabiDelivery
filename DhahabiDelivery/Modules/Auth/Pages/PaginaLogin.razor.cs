using DhahabiDelivery.Modules.Auth.Exceptions;
using DhahabiDelivery.Modules.Auth.Models;
using DhahabiDelivery.Modules.Shared.Components.Buttons;
using DhahabiDelivery.Modules.Shared.Components.Dialogs;
using DhahabiDelivery.Shared.Resources;
using Microsoft.AspNetCore.Components;

namespace DhahabiDelivery.Modules.Auth.Pages;

public partial class PaginaLogin
{
    private Dialog _dialog;
    private string _errorMessage = string.Empty;
    private LoadingButton.State _isLoading = LoadingButton.State.Normal;

    [SupplyParameterFromForm] private ModeloLogin? Model { get; set; }

    protected override void OnInitialized()
    {
        Model ??= new ModeloLogin();
    }

    private async Task OnValidSubmit()
    {
        _isLoading = LoadingButton.State.Loading;
        try
        {
            var token = await AuthService.ValidateLoginAsync(Model);
            if (token != null) Nm.NavigateTo(Configuration.Pages.Home);
        }
        catch (LoginException ex)
        {
            if (ex.Message == Language[Idioma.SuEmailNoHaSidoConfirmado])
            {
                ConfirmarEmail();
                return;
            }

            _errorMessage = ex.Message;
            await _dialog.Open();
        }

        _isLoading = LoadingButton.State.Normal;
    }

    private void ConfirmarEmail()
    {
        Nm.NavigateTo($"{Configuration.Pages.ConfirmarEmail}/{ConfirmarEmailReason.Login}?email={Model?.Email}");
    }
}