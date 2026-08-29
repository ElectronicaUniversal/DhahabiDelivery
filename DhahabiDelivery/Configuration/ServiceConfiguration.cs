using DhahabiDelivery.Configuration.Transition;
using DhahabiDelivery.Modules.Auth.Services;
using DhahabiDelivery.Modules.Auth.ViewModels;
using DhahabiDelivery.Modules.Entregas.Services;
using DhahabiDelivery.Modules.Entregas.ViewModels;
using DhahabiDelivery.Modules.Layout;
using DhahabiDelivery.Modules.Shared;
using DhahabiDelivery.Modules.Shared.Services;
using FrontentCompartido.Modules.Shared.Services;
using GoogleMapsComponents;
using Microsoft.AspNetCore.Components.Authorization;

namespace DhahabiDelivery.Configuration;

public static class ServiceConfiguration
{
    public static void ConfigureServices(this IServiceCollection services, AppSettings? config)
    {
        services.AddScoped<IStorageService, StorageService>();
        AddHttpClients(services, config);
        services.AddLocalization();
        if (config != null) services.AddSingleton(config);
        services.AddViewTransition();
        services.AddAuthorizationCore();
        services.AddScoped<AuthService>();
        services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<AuthService>());
        services.AddBlazorGoogleMaps("AIzaSyAbvNkYW7heICE8HC96_R_UyW-BSdkMDnI");
        services.AddScoped<HttpHelper>();
        services.AddScoped<IImageService, ImageService>();
        
        // Registrar el servicio de ubicación como singleton para acceso global
        services.AddSingleton<LocationService>();
        
        
        AddServices(services);
        AddViewModels(services);
    }

    private static void AddServices(IServiceCollection services)
    {
#if MOCK
        services.AddScoped<IEntregasService, EntregasServiceMock>();
        services.AddScoped<IGeneralesService, GeneralesServiceMock>();
        services.AddScoped<IRepartidorService, RepartidorServiceMock>();
#else
        services.AddScoped<IGeneralesService, GeneralesService>();
        services.AddScoped<IEntregasService, EntregasService>();
        services.AddScoped<IRepartidorService, RepartidorService>();
#endif
    }

    private static void AddViewModels(IServiceCollection services)
    {
        services.AddScoped<MainViewModel>();
        services.AddScoped<EntregasViewModel>();
        services.AddScoped<ConfirmarEmailViewModel>();
    }

    private static void AddHttpClients(IServiceCollection services, AppSettings? settings)
    {
        if (settings == null) return;
        services.AddHttpClient(Apis.AuthenticationQuery.Name,
            client => client.BaseAddress = new Uri(settings.AutenticationQuery));
        services.AddHttpClient(Apis.AuthenticationCommand.Name,
            client => client.BaseAddress = new Uri(settings.AutenticationCommand));
        services.AddHttpClient(Apis.VentasQuery.Name, client => client.BaseAddress = new Uri(settings.VentasQuery));
        services.AddHttpClient(Apis.AgentesQuery.Name, client => client.BaseAddress = new Uri(settings.AgentesQuery));
        services.AddHttpClient(Apis.AgentesCommand.Name,
            client => client.BaseAddress = new Uri(settings.AgentesCommand));
        services.AddHttpClient(Apis.GeneralesQuery.Name,
            client => client.BaseAddress = new Uri(settings.GeneralesQuery));
    }
}