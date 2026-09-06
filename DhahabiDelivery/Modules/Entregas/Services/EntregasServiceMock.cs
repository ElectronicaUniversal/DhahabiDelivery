using DhahabiDelivery.Modules.Shared.Services;
using Mensajeria;

namespace DhahabiDelivery.Modules.Entregas.Services;

public class EntregasServiceMock(HttpHelper httpHelper) : IEntregasService
{
    public new Task<EntregaResumen[]> ObtenerEntregas(CancellationToken cancellationToken)
    {
        var vendedor = new VendedorEntregaResumen(1, "vendedor", "imagen", "direccion del vendedor");
        var producto = new ProductoEntregaResumen
        {
            Id = 1,
            Nombre = "name",
            Imagen = "imagen",
            Precio = 12,
            Cantidad = 3,
            Vendedor = vendedor
        };
        var entrega = new EntregaResumen([producto], "CUP", "CASH", [vendedor], 3, 0, 0, "53234234234", 5,
            "pepito el flaquito",
            "imagenCliente",
            "direccionCliente", "21.2312123, 12.32131234", ConstantesEstadoEnvio.ENCAMINO);
        return Task.FromResult<EntregaResumen[]>([entrega]);
    }
}