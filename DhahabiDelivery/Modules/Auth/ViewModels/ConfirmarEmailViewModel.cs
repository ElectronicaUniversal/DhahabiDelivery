using DhahabiDelivery.Configuration;
using DhahabiDelivery.Modules.Auth.Models;
using DhahabiDelivery.Modules.Auth.Services;
using DhahabiDelivery.Shared.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace DhahabiDelivery.Modules.Auth.ViewModels;

public partial class ConfirmarEmailViewModel(
    NavigationManager nm,
    IStringLocalizer<Idioma> language,
    AuthService authService)
{
    [ObservableProperty] private string _decodedEmail = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _reason = string.Empty;
    [ObservableProperty] private bool _sendEmailLoading;
    [ObservableProperty] private string _token = string.Empty;

    public async void SendEmail()
    {
        SendEmailLoading = true;
        if (Reason == ConfirmarEmailReason.ChangePassword)
        {
            await authService.SolicitarCambioContrasenia(DecodedEmail);
            SendEmailLoading = false;
            return;
        }

        try
        {
            await authService.ReenviarCorreoAlRegistrarse(DecodedEmail);
        }
        catch
        {
            ErrorMessage = language[Idioma.ErrorDesconocido];
        }

        SendEmailLoading = false;
    }

    public async Task ConfirmarEmail()
    {
        if (string.IsNullOrEmpty(Token)) return;
        IsLoading = true;
        try
        {
            await authService.ConfirmarEmail(DecodedEmail, Token);
            nm.NavigateTo(Configuration.Pages.Home);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        IsLoading = false;
    }
}