namespace DhahabiDelivery.Configuration;

public static class Pages
{
    public const string Home = "/";
    public const string Login = "/login";
    public const string Registro = "/register";
    public const string OlvidarContrasenia = "/olvidar-contrasenia";
    public const string OlvidarContraseniaEmail = "/olvidar-contrasenia/{email}";
    public const string ConfirmarEmail = "/confirmar-email";
    public const string ConfirmarEmailReason = $"{ConfirmarEmail}/{{reason}}";
    public const string Usuario = "/usuario";
    public const string EntregasDetail = "/entregas";
    public const string EntregasDetailId = "/entregas/{id:int}";
    public const string Ayuda = "/ayuda";
    public const string CambiarContrasenia = "/cambiar-contrasenia";
    public const string Scanner = "/scanner";
    public const string ScannerResult = "/scanner-result";
    public const string DeliveryStatus = "/delivery-status";
}