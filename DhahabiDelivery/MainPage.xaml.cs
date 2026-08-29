using DhahabiDelivery.Modules;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace DhahabiDelivery;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (stackLayout.Children.Count != 0) return;
        blazorWebView = new BlazorWebView
        {
            HostPage = "wwwroot/index.html",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand
        };

        RootComponent rootComponent = new()
        {
            Selector = "#app",
            ComponentType = typeof(Routes)
        };

        blazorWebView.RootComponents.Add(rootComponent);
        stackLayout.Children.Add(blazorWebView);
    }

    protected override void OnDisappearing()
    {
        // SOLUCIÓN SIMPLE: Solo desmontamos el WebView si NO hay una CameraPage modal activa
        // Esto preserva el contexto durante operaciones de escáner QR
        // manteniendo el fix para el bug de MAUI en otros casos
        var currentModal = Navigation?.ModalStack?.LastOrDefault();
        var isCameraPageActive = currentModal is CameraPage;

        if (!isCameraPageActive) stackLayout.Children.Remove(blazorWebView);

        base.OnDisappearing();
    }
}