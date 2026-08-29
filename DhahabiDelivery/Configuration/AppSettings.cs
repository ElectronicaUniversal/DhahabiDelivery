namespace DhahabiDelivery.Configuration;

public class AppSettings
{
    public string ImageServer { get; init; } = string.Empty;
    public string VentasCommand { get; init; } = string.Empty;
    public string VentasQuery { get; init; } = string.Empty;
    public string AgentesQuery { get; init; } = string.Empty;
    public string AgentesCommand { get; init; } = string.Empty;
    public string AutenticationQuery { get; init; } = string.Empty;
    public string AutenticationCommand { get; init; } = string.Empty;
    public string Catalogo { get; init; } = string.Empty;
    public string PagosCommand { get; init; } = string.Empty;
    public string PagosCubaCommand { get; init; } = string.Empty;
    public string PagosQuery { get; init; } = string.Empty;
    public string ClientesQuery { get; init; } = string.Empty;
    public string GeneralesQuery { get; init; } = string.Empty;
    public string GeneralesCommand { get; init; } = string.Empty;
    public string PromocionesQuery { get; init; } = string.Empty;
}