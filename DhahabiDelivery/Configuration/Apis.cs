namespace DhahabiDelivery.Configuration;

public static class Apis
{
    public static class VentasCommand
    {
        public const string Name = "VentasCommand";
        public const string CrearActualizarOrdenRequest = "Venta/CrearActualizarOrden";

        public const string EliminarOrdenHistorialCliente =
            "Orden/EliminarOrdenHistorialCliente";
    }

    public static class VentasQuery
    {
        public const string Name = "VentasQuery";
        public const string ObtenerEntregasResumenLista = "Entrega/ObtenerEntregasResumenLista";
        public const string ObtenerOrdenCarritoCliente = "Ordenes/ObtenerOrdenCarritoCliente";
        public const string ObtenerPedidoCliente = "Ordenes/ObtenerPedidoCliente";
        public const string ObtenerCostoDelivery = "Ordenes/ObtenerCostoDelivery";
        public const string ObtenerOrdenesAfectacionPuntos = "Ordenes/ObtenerOrdenesAfectacionPuntos";
        public const string ObtenerPuntosVentaDisponiblesOrden = "MetodosEnvio/ObtenerPuntosVentaDisponiblesOrden";
    }

    public static class AuthenticationQuery
    {
        public const string Name = "Authentication";
        public const string ObtenerNuevoToken = "Autenticacion/ObtenerNuevoToken";
        public const string IniciarSesion = "Autenticacion/IniciarSesion";
        public const string ConfirmarEmail = "Autenticacion/ConfirmarEmail";
    }

    public static class AuthenticationCommand
    {
        public const string Name = "AuthenticationCommand";
        public const string RegistrarUsuario = "Autenticacion/RegistrarUsuario";
        public const string RecuperarCuentaCrearEmail = "Autenticacion/RecuperarCuentaCrearEmail";
        public const string EnviarNuevoCorreoConfirmacion = "Autenticacion/EnviarNuevoCorreoConfirmacion";
        public const string EliminarCliente = "Autenticacion/EliminarCliente";
        public const string CrearCorreoEliminarCliente = "Autenticacion/CrearCorreoEliminarCliente";
        public const string CambiarContrasenaCliente = "Autenticacion/CambiarContrasenaCliente";
    }

    public static class PagosCommand
    {
        public const string Name = "PagosCommand";
        public const string CrearPagoEnzona = "Enzona/CrearPagoEnzona/CrearPagoEnzona";
        public const string ConfirmarPagoEnzona = "Enzona/ConfirmarPagoEnzona/ConfirmarPagoEnzona";
        public const string CrearPeticionCrearPagoEnzona = "Enzona/CrearPeticionCrearPagoEnzona";
        public const string CrearPeticionObtenerTokenEnzona = "Enzona/CrearPeticionObtenerTokenEnzona";
        public const string ProcesarObtenerTokenEnzona = "Enzona/ProcesarObtenerTokenEnzona";
    }

    public static class PagosCubaCommand
    {
        public const string Name = "PagosCubaCommand";
        public const string CrearPagoEnzona = "Enzona/CrearPagoEnzona/CrearPagoEnzona";
        public const string ConfirmarPagoEnzona = "Enzona/ConfirmarPagoEnzona/ConfirmarPagoEnzona";
        public const string CrearPeticionCrearPagoEnzona = "Enzona/CrearPeticionCrearPagoEnzona";
        public const string CrearPeticionObtenerTokenEnzona = "Enzona/CrearPeticionObtenerTokenEnzona";
        public const string ProcesarTokenEnzona = "Enzona/ProcesarTokenEnzona";
        public const string CrearPeticionCancelarPagoEnzona = "Enzona/CrearPeticionCancelarPagoEnzona";
        public const string CrearPeticionConfirmarPagoEnzona = "Enzona/CrearPeticionConfirmarPagoEnzona";
        public const string ProcesarCancelarPagoEnzona = "/Enzona/ProcesarCancelarPagoEnzona";
        public const string ProcesarConfirmarPagoEnzona = "Enzona/ProcesarConfirmarPagoEnzona";
        public const string ProcesarConfirmarPagoEnzonaFactura = "Enzona/ProcesarConfirmarPagoFacturaEnzona";
        public const string ProcesarCrearPagoEnzona = "/Enzona/ProcesarCrearPagoEnzona";
    }

    public static class PagosQuery
    {
        public const string Name = "PagosQuery";
        public const string ObtenerMetodosPago = "MetodosPago/ObtenerMetodosPago";
    }

    public static class Catalogo
    {
        public const string Name = "Catalogo";
        public const string Categorias = "Categoria/ObtenerCategoriaItemLista";
        public const string ObtenerTodosLosProductos = "Producto/ObtenerProductoResumenListaTienda";
        public const string ObtenerDetalleProducto = "Producto/ObtenerDetalleProducto";
        public const string ObtenerProductosFiltradosResumenLista = "Producto/ObtenerProductosFiltradosResumenLista";
        public const string ObtenerCategoriasMasVendidasItemLista = "Categoria/ObtenerCategoriasMasVendidasItemLista";

        public const string ObtenerProductosMasVendidosResumenLista =
            "Producto/ObtenerProductosMasVendidosResumenLista";
    }

    public static class ClientesQuery
    {
        public const string Name = "ClientesQuery";
        public const string ObtenerDireccionResumenListaPorClient = "Clientes/ObtenerDireccionResumenListaPorCliente";
        public const string ObtenerPuntosCliente = "Clientes/ObtenerPuntosCliente";
    }

    public static class GeneralesQuery
    {
        public const string Name = "GeneralesQuery";
        public const string ObtenerListaPaises = "Pais/ObtenerListaPaises";
        public const string ObtenerListaProvincias = "Provincia/ObtenerListaProvincias";
        public const string ObtenerListaMunicipiosIdProvincia = "Direccion/ObtenerListaMunicipiosIdProvincia";
        public const string ObtenerVersionAppResumen = "VersionApp/ObtenerVersionAppResumen";
        public const string ObtenerDireccionRecogidaTienda = "Direccion/ObtenerDireccionRecogidaTienda";
        public const string ObtenerTasaCambioResumenLista = "Moneda/ObtenerTasaCambioResumenLista";

        public const string ObtenerDireccionResumenListaPorCliente =
            "Direccion/ObtenerDireccionResumenListaPorCliente";
    }

    public static class GeneralesCommand
    {
        public const string Name = "GeneralesCommand";
        public const string NuevoActualizaDireccion = "Direccion/NuevoActualizaDireccion";
        public const string EliminarDireccion = "Direccion/EliminarDireccion";
    }

    public static class PromocionesQuery
    {
        public const string Name = "PromocionesQuery";
        public const string ObtenerPublicidadItemLista = "Publicidad/ObtenerPublicidadItemLista";
    }

    public static class AgentesQuery
    {
        public const string Name = "AgentesQuery";
        public const string ObtenerEstadoRepartidor = "Repartidor/ObtenerEstadoRepartidor";
    }

    public static class AgentesCommand
    {
        public const string Name = "AgentesCommand";
        public static string EstablecerEstadoRepartidor = "Repartidor/EstablecerEstadoRepartidor";
        public static string ActualizarPosicionRepartidor = "Repartidor/ActualizarPosicionRepartidor";
    }
}