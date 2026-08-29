using System.Net;
using DhahabiDelivery.Modules.Auth.Models;
using DhahabiDelivery.Modules.Auth.Services;
using DhahabiDelivery.Modules.Shared.Components.Buttons;
using Microsoft.AspNetCore.Components;

namespace DhahabiDelivery.Modules.Auth.Pages;

public partial class PaginaRegistro
{
    private string _errorMessage = string.Empty;
    private LoadingButton.State _isLoading = LoadingButton.State.Normal;
    [SupplyParameterFromForm] private ModeloRegistro? Model { get; set; }
    [Inject] private AuthService? AuthService { get; set; }

    private void DismissErrorModal()
    {
        _errorMessage = string.Empty;
    }

    protected override void OnInitialized()
    {
        Model ??= new ModeloRegistro();
    }

    private async Task OnValidSubmit()
    {
        if (AuthService == null) return;
        _isLoading = LoadingButton.State.Loading;
        try
        {
            await AuthService.RegisterUserAsync(Model);
            var encodedEmail = WebUtility.UrlEncode(Model?.Email);
            Nm.NavigateTo($"confirmar-email/{ConfirmarEmailReason.Register}?email={encodedEmail}");
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            _isLoading = LoadingButton.State.Normal;
            StateHasChanged();
        }
    }
}