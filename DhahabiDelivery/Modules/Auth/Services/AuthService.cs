using System.Globalization;
using System.Net.Http.Json;
using System.Security.Claims;
using DhahabiDelivery.Configuration;
using DhahabiDelivery.Modules.Auth.Exceptions;
using DhahabiDelivery.Modules.Auth.Models;
using DhahabiDelivery.Modules.Shared.Models;
using DhahabiDelivery.Modules.Shared.Services;
using DhahabiDelivery.Shared.Resources;
using Mensajeria;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;

namespace DhahabiDelivery.Modules.Auth.Services;

/// <summary>
///     Es responsable de gestionar la autenticación de los usuarios en la aplicación.
///     Se encarga de manejar el inicio de sesión, el cierre de sesión,
///     la renovación de tokens y la verificación del estado de autenticación del usuario.
/// </summary>
public class AuthService : AuthenticationStateProvider
{
    private const string TokenKey = "accounttoken";
    private const string RefreshTokenKey = "refeshtoken";
    private const string StateKey = "delivery_state";
    private readonly IHttpClientFactory _httpClientFactory;


    private readonly IStorageService _storageService;

    /// <inheritdoc />
    public AuthService(IStorageService storageService, IHttpClientFactory httpClientFactory,
        IStringLocalizer<Idioma> language)
    {
        Language = language;
        _storageService = storageService;
        _storageService = storageService;
        _httpClientFactory = httpClientFactory;
    }

    private IStringLocalizer<Idioma> Language { get; }

    private ClaimsPrincipal? ClaimsPrincipal { get; set; }

    /// <summary>
    ///     Token necesario para las llamadas seguras
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    ///     Modelo encargado de almacenar los datos del usuario.
    /// </summary>
    public ModeloUsuario? User { get; private set; }

    public string GetDeliveryStateAsync()
    {
        return _storageService.GetAsync(StateKey) ?? string.Empty;
    }

    public void SetDeliveryStateAsync(string state)
    {
        _storageService.SetAsync(StateKey, state);
    }

    /// <summary>
    ///     Obtiene el estado de autenticación actual del usuario.
    ///     Si hay un token en el almacenamiento seguro, el usuario está autenticado.
    /// </summary>
    /// <returns>El estado de autenticación del usuario.</returns>
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            Token = _storageService.GetAsync(TokenKey) ?? string.Empty;
            User = _storageService.GetActiveUser();

            if (!string.IsNullOrEmpty(Token))
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, User?.NickName ?? ""),
                    new(ClaimTypes.Name, User?.Name ?? ""),
                    new(ClaimTypes.Surname, User?.LastName ?? ""),
                    new(ClaimTypes.Gender, User?.EsMasculino == true ? "true" : "false"),
                    new(ClaimTypes.DateOfBirth, User?.FechaNacimiento ?? ""),
                    new(ClaimTypes.Email, User?.Email ?? "")
                };

                var identity = new ClaimsIdentity(claims, "Custom authentication");
                ClaimsPrincipal = new ClaimsPrincipal(identity);
                return Task.FromResult(new AuthenticationState(ClaimsPrincipal));
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
    }

    /// <summary>
    ///     Cierra la sesión del usuario eliminando el token y el refresh token almacenados.
    ///     Notifica el cambio de estado de autenticación.
    /// </summary>
    /// <returns>Tarea asincrónica.</returns>
    public void Logout()
    {
        _storageService.Remove(TokenKey);
        _storageService.Remove(RefreshTokenKey);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    ///     Valída el inicio de sesión del usuario.
    ///     Envía una solicitud de autenticación y guarda la información del usuario y el token si es exitosa.
    /// </summary>
    /// <param name="model">Modelo de inicio de sesión que contiene el email y la contraseña.</param>
    /// <returns>El token de autenticación si es exitoso, de lo contrario, null.</returns>
    public async Task<string?> ValidateLoginAsync(ModeloLogin? model)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient(Apis.AuthenticationQuery.Name);
            if (model == null) return null;
            var req = new IniciarSesionClienteRequest(model.Email ?? "", model.Password ?? "");
            var response = await httpClient.PostAsJsonAsync(Apis.AuthenticationQuery.IniciarSesion, req);

            if (!response.IsSuccessStatusCode) throw new LoginException(Language[Idioma.HaOcurridoUnErrorDeConexion]);

            var res = await response.Content.ReadFromJsonAsync<IniciarSesionClienteResponse>() ??
                      throw new LoginException(Language[Idioma.HaOcurridoUnErrorDesconocido]);

            if (res.Token.Token == "EmailNoConfirmado")
                throw new LoginException(Language[Idioma.SuEmailNoHaSidoConfirmado]);
            if (string.IsNullOrEmpty(res.Token.Token))
                throw new LoginException(Language[Idioma.UsuarioOContraseniaIncorrectos]);

            await SaveInfoCliente(res.InfoCliente, model);
            Login(res.Token);
            return res.Token.Token;
        }
        catch (HttpRequestException)
        {
            throw new LoginException(Language[Idioma.HaOcurridoUnErrorDeConexion]);
        }
        catch (Exception ex)
        {
            if (ex is LoginException) throw;
            throw new LoginException(Language[Idioma.HaOcurridoUnErrorDesconocido]);
        }
    }

    /// <summary>
    ///     Solicita el reenvío de un correo de confirmación al email proporcionado.
    /// </summary>
    /// <param name="email">El email del usuario que necesita la confirmación.</param>
    /// <returns>Código de respuesta del servidor.</returns>
    public async Task ReenviarCorreoAlRegistrarse(string email)
    {
        var httpClient = _httpClientFactory.CreateClient(Apis.AuthenticationCommand.Name);
        var req = new EnviarNuevoCorreoConfirmacionRequest(email);
        var res = await httpClient.PostAsJsonAsync(Apis.AuthenticationCommand.EnviarNuevoCorreoConfirmacion, req);
        res.EnsureSuccessStatusCode();
        var response = await res.Content.ReadFromJsonAsync<EnviarNuevoCorreoConfirmacionResponse>();
        _ = response == null ? throw new LoginException() : response.Codigo;
    }

    /// <summary>
    ///     Confirma el email del usuario utilizando un token de confirmación.
    /// </summary>
    /// <param name="email">El email del usuario a confirmar.</param>
    /// <param name="token">El token de confirmación proporcionado.</param>
    /// <returns>Tarea asincrónica.</returns>
    public async Task ConfirmarEmail(string email, string token)
    {
        var httpClient = _httpClientFactory.CreateClient(Apis.AuthenticationQuery.Name);
        var req = new ConfirmarEmailClienteRequest(token, email);
        try
        {
            var res = await httpClient.PostAsJsonAsync(Apis.AuthenticationQuery.ConfirmarEmail, req);

            if (!res.IsSuccessStatusCode)
                throw new LoginException(Language[Idioma.HaOcurridoUnErrorDeConexion]);

            var response = await res.Content.ReadFromJsonAsync<ConfirmarEmailClienteResponse>() ??
                           throw new LoginException(Language[Idioma.HaOcurridoUnErrorDesconocido]);

            if (string.IsNullOrEmpty(response.Token.Token) || string.IsNullOrEmpty(response.Token.RefreshToken))
                throw new LoginException(Language[Idioma.TokenIncorrecto]);

            await SaveInfoCliente(response.InfoCliente);
            Login(response.Token);
        }
        catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.ConnectionError)
        {
            throw new LoginException(Language[Idioma.HaOcurridoUnErrorDeConexion]);
        }
        catch (Exception ex)
        {
            if (ex is LoginException) throw;
            throw new LoginException(Language[Idioma.HaOcurridoUnErrorDesconocido]);
        }
    }

    /// <summary>
    ///     Solicita un cambio de contraseña enviando un email al usuario.
    /// </summary>
    /// <param name="email">El email del usuario que solicita el cambio.</param>
    /// <returns>Código de respuesta del servidor.</returns>
    public async Task<string?> SolicitarCambioContrasenia(string email)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient(Apis.AuthenticationCommand.Name);
            var req = new RecuperarCuentaCrearEmailRequest(email);
            var res = await httpClient.PostAsJsonAsync(Apis.AuthenticationCommand.RecuperarCuentaCrearEmail, req);
            if (!res.IsSuccessStatusCode) return string.Empty;
            var response = await res.Content.ReadFromJsonAsync<RecuperarCuentaCrearEmailResponse>();
            return response?.Codigo;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>
    ///     Inicia sesión en la aplicación almacenando el token JWT en el almacenamiento seguro.
    ///     Notifica a .NET que el estado de autenticación ha cambiado.
    /// </summary>
    /// <param name="token">El token JWT a almacenar.</param>
    /// <returns>Tarea asincrónica.</returns>
    private void Login(RefrescarTokenResponse token)
    {
        _storageService.SetAsync(TokenKey, token.Token);
        _storageService.SetAsync(RefreshTokenKey, token.RefreshToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    ///     Renueva los tokens de acceso y refresh del usuario.
    /// </summary>
    /// <returns>El nuevo RefreshToken.</returns>
    public async Task<RefrescarTokenResponse> RefreshTokens()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient(Apis.AuthenticationQuery.Name);
            var refreshToken = _storageService.GetAsync(RefreshTokenKey);
            if (refreshToken == null)
            {
                var modeloLogin = new ModeloLogin
                {
                    Email = User?.Email ?? string.Empty,
                    Password = User?.Password ?? string.Empty
                };
                await ValidateLoginAsync(modeloLogin);
                return new RefrescarTokenResponse(string.Empty, string.Empty);
            }

            var req = new RefrescarTokenRequest(refreshToken);
            var res = await httpClient.PostAsJsonAsync(Apis.AuthenticationQuery.ObtenerNuevoToken, req);
            res.EnsureSuccessStatusCode();
            var response = await res.Content.ReadFromJsonAsync<RefrescarTokenResponse>();
            if (response == null) throw new LoginException("No se puede obtener el token");
            if (string.IsNullOrEmpty(response.Token) || string.IsNullOrEmpty(response.RefreshToken))
                throw new LoginException("El nuevo token o refresh token no es valido");
            Login(response);
            return response;
        }
        catch (LoginException ex)
        {
            Console.WriteLine(ex.Message);
            Logout();
            throw;
        }
    }

    /// <summary>
    ///     Guarda la información del cliente en el almacenamiento.
    /// </summary>
    /// <param name="infoCliente">Información del cliente a guardar.</param>
    /// <param name="loginModel"></param>
    /// <returns>Tarea asincrónica.</returns>
    private async Task SaveInfoCliente(ClienteResumenCuentaTienda infoCliente, ModeloLogin? loginModel = null)
    {
        ModeloUsuario model = new(
            infoCliente.Codigo,
            infoCliente.Nombre,
            infoCliente.Apellido,
            infoCliente.EsMasculino,
            infoCliente.FechaNacimiento.ToString(CultureInfo.InvariantCulture),
            infoCliente.Email,
            loginModel?.Password
        );
        _storageService.SaveActiveUser(model);
    }

    /// <summary>
    ///     Registra un nuevo usuario en la aplicación.
    /// </summary>
    /// <param name="model">Modelo de registro que contiene la información del nuevo usuario.</param>
    /// <returns>Código del nuevo cliente si el registro es exitoso.</returns>
    public async Task RegisterUserAsync(ModeloRegistro? model)
    {
        try
        {
            if (model == null) throw new LoginException();
            var httpClient = _httpClientFactory.CreateClient(Apis.AuthenticationCommand.Name);
            var req = model.ToRequest();
            var res = await httpClient.PostAsJsonAsync(Apis.AuthenticationCommand.RegistrarUsuario, req);
            res.EnsureSuccessStatusCode();
            var response = await res.Content.ReadFromJsonAsync<RegistrarClienteResponse>() ??
                           throw new LoginException(Language["ErrorDesconocido"]);

            if (string.IsNullOrEmpty(response.Token.Token) || response.Token.Token == "UsuarioExiste")
                throw new LoginException(Language["ElEmailYaExiste"]);
        }
        catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.ConnectionError)
        {
            throw new LoginException(Language[Idioma.HaOcurridoUnErrorDeConexion]);
        }
        catch (Exception ex)
        {
            if (ex is LoginException) throw;
            throw new LoginException(Language[Idioma.HaOcurridoUnErrorDesconocido]);
        }
    }

    /// <summary>
    ///     Elimina la cuenta del cliente especificado.
    /// </summary>
    /// <param name="codigoCliente">Código del cliente a eliminar.</param>
    /// <returns>Tarea asincrónica.</returns>
    public async Task EliminarCuenta(string codigoCliente)
    {
        var req = new EliminarClienteRequest(codigoCliente);
        var httpClient = _httpClientFactory.CreateClient(Apis.AuthenticationCommand.Name);
        var res = await httpClient.PostAsJsonAsync(Apis.AuthenticationCommand.EliminarCliente, req);
        res.EnsureSuccessStatusCode();
        var response = await res.Content.ReadFromJsonAsync<EliminarClienteResponse>();
        if (response == null) throw new Exception();
        Logout();
    }
}