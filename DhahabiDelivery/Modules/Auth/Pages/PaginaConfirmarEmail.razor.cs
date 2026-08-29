using System.Net;
using Microsoft.AspNetCore.Components;

namespace DhahabiDelivery.Modules.Auth.Pages;

public partial class PaginaConfirmarEmail
{
    private string _token = string.Empty;
    [SupplyParameterFromQuery] public string? Email { get; set; } = string.Empty;
    [SupplyParameterFromQuery] public string? Token { get; set; } = string.Empty;
    [Parameter] public string Reason { get; set; } = string.Empty;

    private void DismissErrorModal()
    {
        ViewModel.ErrorMessage = string.Empty;
    }

    protected override async Task OnParametersSetAsync()
    {
        ViewModel.DecodedEmail = WebUtility.UrlDecode(Email);
        ViewModel.Token = Token ?? string.Empty;
        _token = Token ?? string.Empty;
        ViewModel.Reason = Reason;
        await ViewModel.ConfirmarEmail();
    }
}