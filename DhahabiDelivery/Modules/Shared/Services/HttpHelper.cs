using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DhahabiDelivery.Modules.Auth.Services;
using MediatR;
using Polly;
using Polly.Retry;

namespace DhahabiDelivery.Modules.Shared.Services;

/// <summary>
///     Clase auxiliar para realizar solicitudes HTTP.
/// </summary>
/// <param name="authService">Servicio de autenticación para obtener el token.</param>
/// <param name="httpClientFactory">Fábrica de clientes HTTP para crear instancias de HttpClient.</param>
public class HttpHelper(AuthService authService, IHttpClientFactory httpClientFactory)
{
    private readonly AsyncRetryPolicy _retryPolicy = Policy.Handle<HttpRequestException>().WaitAndRetryAsync(2,
        retryAttempt =>
            TimeSpan.FromSeconds(retryAttempt * 2)
    );

    /// <summary>
    ///     Realiza una solicitud HTTP POST con el objeto de solicitud dado y devuelve la respuesta.
    /// </summary>
    /// <typeparam name="TRequest">El tipo del objeto de solicitud.</typeparam>
    /// <typeparam name="TResponse">El tipo del objeto de respuesta.</typeparam>
    /// <param name="request">El objeto de solicitud que se enviará en la solicitud POST.</param>
    /// <param name="config">La configuración de la API y la URL a utilizar.</param>
    /// <param name="cancellationToken">Un token para cancelar la operación.</param>
    /// <returns>El objeto de respuesta deserializado de la respuesta JSON.</returns>
    /// <exception cref="HttpRequestException">Se lanza cuando la solicitud HTTP falla.</exception>
    /// <exception cref="InvalidOperationException">Se lanza cuando el contenido de la respuesta es nulo.</exception>
    public async Task<TResponse> MakeHttpRequestAsync<TRequest, TResponse>(TRequest request, HttpHelperConfig config,
        CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient(config.Api);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authService.Token);

        var response = await client.PostAsJsonAsync(config.Url, request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var res = await authService.RefreshTokens();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", res.Token);
            response = await client.PostAsJsonAsync(config.Url, request, cancellationToken);
        }

        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        if (responseString == null) throw new InvalidOperationException("El contenido de la respuesta es nulo.");

        return responseString;
    }

    public async Task<TResponse> HttpRequestAsync<TResponse>(
        IRequest<TResponse> request,
        HttpHelperConfig config,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
            await MakeHttpRequestAsync<IRequest<TResponse>, TResponse>(request, config, cancellationToken));
    }

    public async Task<string> GetResponseString<TRequest>(TRequest request, HttpHelperConfig config,
        CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient(config.Api);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authService.Token);
        var response = await client.PostAsJsonAsync(config.Url, request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var res = await authService.RefreshTokens();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", res.Token);
            response = await client.PostAsJsonAsync(config.Url, request, cancellationToken);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task MakeHttpCommand<TRequest>(TRequest request, HttpHelperConfig config,
        CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient(config.Api);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authService.Token);

        var response = await client.PostAsJsonAsync(config.Url, request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var res = await authService.RefreshTokens();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", res.Token);
            response = await client.PostAsJsonAsync(config.Url, request, cancellationToken);
        }

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    ///     Clase para configurar la API y la URL para las solicitudes HTTP.
    /// </summary>
    public class HttpHelperConfig()
    {
        public HttpHelperConfig(string api, string url) : this()
        {
            Api = api;
            Url = url;
        }

        /// <summary>
        ///     El nombre de la API a utilizar.
        /// </summary>
        public string Api { get; init; } = string.Empty;

        /// <summary>
        ///     La URL a la que se enviará la solicitud.
        /// </summary>
        public string Url { get; init; } = string.Empty;
    }
}