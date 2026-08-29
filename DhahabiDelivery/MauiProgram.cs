using System.Reflection;
using DhahabiDelivery.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace DhahabiDelivery;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("DhahabiDelivery.Configuration.AppSettings.json");
        var config = stream != null ? builder.Configuration.AddJsonStream(stream).Build().Get<AppSettings>() : null;
        builder.Services.ConfigureServices(config);
#if DEBUG || MOCK
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}