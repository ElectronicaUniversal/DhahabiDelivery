using Mensajeria;

namespace DhahabiDelivery.Configuration;

public static class MetodosExtension
{
    private static decimal NormalizarDecimal(this decimal valor)
    {
        return valor / 1.000000000000000000000000000000000m;
    }

    public static decimal GetPrice(this decimal precio, TasaCambioResumen? tasaCambio)
    {
        var res = (tasaCambio?.Valor ?? 1) * precio;
        return res.NormalizarDecimal();
    }
}