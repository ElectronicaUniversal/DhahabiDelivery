using ZXing.Net.Maui;

namespace DhahabiDelivery;

public partial class CameraPage
{
    private readonly TaskCompletionSource<BarcodeResult[]> _scanTask = new();
    private bool _hasScanned;

    public CameraPage()
    {
        InitializeComponent();

        CameraBarcodeReaderView.Options = new BarcodeReaderOptions
        {
            // Include QR code format
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _hasScanned = false;

        // Request camera permissions if needed
        RequestCameraPermissionIfNeeded();
    }

    private async void RequestCameraPermissionIfNeeded()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status == PermissionStatus.Granted) return;
        status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status == PermissionStatus.Granted) return;
        // Permission denied, close the scanner
        await DisplayAlert("Permiso denegado", "Se requiere acceso a la cámara para escanear códigos QR.",
            "OK");
        CancelScan();
        await Navigation.PopModalAsync();
    }

    public Task<BarcodeResult[]> WaitForResultAsync()
    {
        return _scanTask.Task;
    }

    private void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        // Prevent multiple detections
        if (_hasScanned || e.Results.Length == 0) return;

        _hasScanned = true;

        MainThread.BeginInvokeOnMainThread(async void () =>
        {
            try
            {
                // Vibrate as feedback for successful scan
                try
                {
                    if (Vibration.Default.IsSupported)
                        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
                }
                catch
                {
                    /* Ignore vibration errors */
                }

                await Navigation.PopModalAsync();
                _scanTask.TrySetResult(e.Results);
            }
            catch (Exception ex)
            {
                // Handle errors
                _scanTask.TrySetException(ex);
            }
        });
    }

    private void CancelButton_Clicked(object sender, EventArgs e)
    {
        CancelScan();
        Navigation.PopModalAsync();
    }

    private void CancelScan()
    {
        if (!_scanTask.Task.IsCompleted) _scanTask.TrySetResult([]);
    }

    protected override bool OnBackButtonPressed()
    {
        CancelScan();
        return base.OnBackButtonPressed();
    }
}