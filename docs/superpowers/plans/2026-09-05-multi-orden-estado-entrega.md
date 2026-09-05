# Multi-orden y rediseño de estado de entrega — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que un repartidor tenga varias órdenes asignadas en cola, con como máximo una `EnCamino` a la vez, moviendo la fuente de verdad de "qué está pasando con esta entrega" de `Repartidor.Estado` (un solo campo) a `Orden.IdEstadoEnvio` (por orden).

**Architecture:** Backend (`BusinessPlaceServer`): `EstablecerEstadoRepartidorRequest` gana un `IdOrden` opcional (compatible con clientes viejos que no lo mandan); el handler valida ownership de la orden y que no haya otra ya en camino; `AsignarDeliveryHandler` deja de pisar el estado del repartidor si ya está entregando. Cliente (`DhahabiDelivery`): `RepartidorService` manda el `IdOrden` puntual; el ViewModel bloquea "Iniciar" si ya hay otra entrega en camino, usando el nuevo campo `EstadoEnvio` que ahora viaja en `EntregaResumen`.

**Tech Stack:** .NET 8 (BusinessPlaceServer, ASP.NET Core + MediatR + EF Core), .NET 8 MAUI Blazor Hybrid (DhahabiDelivery), xUnit + Moq (test project nuevo, no existía ninguno en el repo del servidor).

**Spec:** `docs/superpowers/specs/2026-09-05-multi-orden-estado-entrega-design.md`

**Repos y ramas:**
- `BusinessPlaceServer`: rama local `fix/delivery-app`, rebasada sobre `origin/dev-stripe` (la rama realmente más actualizada del repo — `origin/dev` estaba desactualizada desde 2026-01-26; `dev-stripe` la contiene por completo más 70 commits adicionales, sin conflicto con los archivos de este trabajo) — usar esta, no crear una nueva.
- `DhahabiDelivery`: flujo normal de ramas del repo (el usuario es mantenedor).

---

## Task 1: Scaffolding del proyecto de tests (BusinessPlaceServer)

No existe ningún proyecto de tests en `BusinessPlaceServer` hoy — este es el primero. Antes de escribir tests reales, hay que probar que el scaffolding compila y corre.

**Files:**
- Create: `BusinessPlaceServer/BusinessPlaceServer.Tests/BusinessPlaceServer.Tests.csproj`
- Create: `BusinessPlaceServer/BusinessPlaceServer.Tests/ScaffoldingTests.cs`
- Modify: `BusinessPlaceServer/BusinessPlaceServer.sln`

- [ ] **Step 1: Crear el proyecto de tests**

Desde `BusinessPlaceServer/`:

```bash
mkdir -p BusinessPlaceServer.Tests
```

Crear `BusinessPlaceServer.Tests/BusinessPlaceServer.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Moq" Version="4.20.72" />
  </ItemGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../PresentationLayer/Microservicios/Command/Agentes.Command.Api/Agentes.Command.Api.csproj" />
    <ProjectReference Include="../PresentationLayer/Microservicios/Command/Ventas.Command.Api/Ventas.Command.Api.csproj" />
  </ItemGroup>

</Project>
```

(El `FrameworkReference` a `Microsoft.AspNetCore.App` es necesario porque este proyecto usa `Microsoft.NET.Sdk` plano, no `Sdk.Web` — sin esto, tipos como `IHttpContextAccessor` no resuelven aunque vengan transitivamente de un `ProjectReference` a un proyecto Web.)

- [ ] **Step 2: Escribir un test trivial para validar el scaffolding**

Crear `BusinessPlaceServer.Tests/ScaffoldingTests.cs`:

```csharp
using Xunit;

namespace BusinessPlaceServer.Tests;

public class ScaffoldingTests
{
    [Fact]
    public void ElProyectoDeTestsCorre()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 3: Agregar el proyecto a la solución**

```bash
dotnet sln BusinessPlaceServer.sln add BusinessPlaceServer.Tests/BusinessPlaceServer.Tests.csproj
```

- [ ] **Step 4: Correr los tests y confirmar que pasa**

```bash
dotnet test BusinessPlaceServer.Tests/BusinessPlaceServer.Tests.csproj
```

Expected: `Passed! - Failed: 0, Passed: 1, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add BusinessPlaceServer.Tests BusinessPlaceServer.sln
git commit -m "Agregar proyecto de tests (xUnit + Moq), primero en el repo"
```

---

## Task 2: Helpers compartidos para construir entidades fake

Las entidades (`Orden`, `Repartidor`) usan propiedades de navegación con lazy-loading de EF Core (`ILazyLoader`). El patrón `_loader.Load(this, ref _campo)` internamente es un no-op seguro si `_loader` es `null` Y el campo ya fue asignado por su setter público (o si ambos son null) — así que se pueden construir instancias fake con `new Orden(...)`/`new Repartidor(...)` y asignar solo las propiedades que el código bajo test necesita, sin tocar `_loader` ni reflection.

**Files:**
- Create: `BusinessPlaceServer.Tests/TestEntityFactory.cs`

- [ ] **Step 1: Escribir el helper compartido**

```csharp
using Agentes;
using Ventas;

namespace BusinessPlaceServer.Tests;

internal static class TestEntityFactory
{
    public static Repartidor CrearRepartidor(int id, string estado) =>
        new Repartidor(
            codigo: "REP1",
            email: "repartidor@test.com",
            emailConfirmado: true,
            numeroTelefono: "555",
            contrasena: "hash",
            nombre: "Test",
            apellido: "Repartidor",
            nombreEmpresa: null,
            esMasculino: true,
            fechaNacimiento: new DateTime(1990, 1, 1),
            comentarioAdmin: null,
            registradoEnTienda: null,
            dirreccionIP: "127.0.0.1",
            fechaCreado: DateTime.UtcNow,
            ultimaActividad: DateTime.UtcNow,
            ultimaPosicion: "",
            foto: "",
            estado: estado)
        { Id = id };

    public static Orden CrearOrden(int id) =>
        new Orden(
            fechaCreacion: DateTime.UtcNow,
            idEstadoOrden: 2,
            idEstadoPago: 1,
            idEstadoEnvio: 1,
            codigoMetodoPago: "CASH",
            codigoMetodoEnvio: "DELIVERY",
            idCliente: 1,
            esEntregable: true,
            idDireccionFacturacion: null,
            idDireccionEnvio: null,
            usaRecompesa: false,
            puntosGenera: 0,
            subtotal: 100,
            puntosTemporales: 0,
            codigoMoneda: "CUP",
            codigoPais: "CU",
            codigoPaisDestino: null)
        { Id = id };
}
```

- [ ] **Step 2: Verificar que compila**

```bash
dotnet build BusinessPlaceServer.Tests/BusinessPlaceServer.Tests.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add BusinessPlaceServer.Tests/TestEntityFactory.cs
git commit -m "Agregar factory de entidades fake para tests de handlers"
```

---

## Task 3: `IdOrden` opcional en `EstablecerEstadoRepartidorRequest`

**Files:**
- Modify: `InfraestructureLayer/Mensajeria/Agentes/RepartidorMensajes.cs:106`

- [ ] **Step 1: Agregar el parámetro opcional**

En `RepartidorMensajes.cs`, reemplazar la línea 106:

```csharp
public record EstablecerEstadoRepartidorRequest(string Estado, int IdRepartidor = 0) : IRequest<EstablecerEstadoRepartidorResponse>;
```

por:

```csharp
public record EstablecerEstadoRepartidorRequest(string Estado, int IdRepartidor = 0, int? IdOrden = null) : IRequest<EstablecerEstadoRepartidorResponse>;
```

- [ ] **Step 2: Verificar que el proyecto Mensajeria compila**

```bash
dotnet build InfraestructureLayer/Mensajeria/Mensajeria.csproj
```

Expected: `Build succeeded.` (Al ser un parámetro opcional al final, ningún call site existente se rompe.)

- [ ] **Step 3: Commit**

```bash
git add InfraestructureLayer/Mensajeria/Agentes/RepartidorMensajes.cs
git commit -m "Agregar IdOrden opcional a EstablecerEstadoRepartidorRequest"
```

---

## Task 4: `EstablecerEstadoRepartidorHandlerHandler` — soporte multi-orden (TDD)

**Files:**
- Modify: `PresentationLayer/Microservicios/Command/Agentes.Command.Api/Handlers/Repartidor/EstablecerEstadoRepartidorHandlerHandler.cs`
- Create: `BusinessPlaceServer.Tests/EstablecerEstadoRepartidorHandlerHandlerTests.cs`

- [ ] **Step 1: Escribir los tests (van a fallar — la lógica nueva no existe todavía)**

Crear `BusinessPlaceServer.Tests/EstablecerEstadoRepartidorHandlerHandlerTests.cs`:

```csharp
using System.Linq.Expressions;
using Agentes;
using Agentes.Command.Handlers;
using Constantes;
using Interfaces;
using Mensajeria;
using Microsoft.AspNetCore.Http;
using Moq;
using Ventas;
using Xunit;

namespace BusinessPlaceServer.Tests;

public class EstablecerEstadoRepartidorHandlerHandlerTests
{
    [Fact]
    public async Task Handle_ConIdOrdenValido_MarcaEsaOrdenComoEnCaminoYRepartidorEntregando()
    {
        var repartidor = TestEntityFactory.CrearRepartidor(id: 10, estado: ConstantesEstadoRepartidor.ASIGNADO);
        var ordenAIniciar = TestEntityFactory.CrearOrden(id: 100);

        var repositoryMock = new Mock<IRepository>();
        repositoryMock
            .SetupSequence(r => r.BpObtenerUno<Orden>(It.IsAny<Expression<Func<Orden, bool>>>()))
            .Returns(ordenAIniciar)
            .Returns((Orden?)null);

        var tokenManagerMock = new Mock<Autorizacion.JwtManager.ITokenManager>();
        tokenManagerMock.Setup(t => t.ObtenerRepartidor(It.IsAny<IHttpContextAccessor>())).Returns(repartidor);

        var handler = new EstablecerEstadoRepartidorHandlerHandler(
            repositoryMock.Object, tokenManagerMock.Object, Mock.Of<IHttpContextAccessor>());

        var request = new EstablecerEstadoRepartidorRequest(ConstantesEstadoRepartidor.ENTREGANDO, IdOrden: 100);

        var response = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(ConstantesEstadoRepartidor.ENTREGANDO, response.EstadoRepartidor);
        ConstantesEstadoEnvio.EstadosEnvio.TryGetValue(ConstantesEstadoEnvio.ENCAMINO, out var idEnCamino);
        Assert.Equal(idEnCamino, ordenAIniciar.IdEstadoEnvio);
    }

    [Fact]
    public async Task Handle_ConIdOrdenQueNoPerteneceAlRepartidor_LanzaInvalidOperationException()
    {
        var repartidor = TestEntityFactory.CrearRepartidor(id: 10, estado: ConstantesEstadoRepartidor.ASIGNADO);

        var repositoryMock = new Mock<IRepository>();
        repositoryMock
            .Setup(r => r.BpObtenerUno<Orden>(It.IsAny<Expression<Func<Orden, bool>>>()))
            .Returns((Orden?)null);

        var tokenManagerMock = new Mock<Autorizacion.JwtManager.ITokenManager>();
        tokenManagerMock.Setup(t => t.ObtenerRepartidor(It.IsAny<IHttpContextAccessor>())).Returns(repartidor);

        var handler = new EstablecerEstadoRepartidorHandlerHandler(
            repositoryMock.Object, tokenManagerMock.Object, Mock.Of<IHttpContextAccessor>());

        var request = new EstablecerEstadoRepartidorRequest(ConstantesEstadoRepartidor.ENTREGANDO, IdOrden: 999);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(request, CancellationToken.None));
        Assert.Equal("La orden 999 no pertenece a este repartidor o no existe.", ex.Message);
    }

    [Fact]
    public async Task Handle_ConOtraOrdenYaEnCamino_LanzaInvalidOperationException()
    {
        var repartidor = TestEntityFactory.CrearRepartidor(id: 10, estado: ConstantesEstadoRepartidor.ENTREGANDO);
        var ordenAIniciar = TestEntityFactory.CrearOrden(id: 100);
        var otraOrdenEnCamino = TestEntityFactory.CrearOrden(id: 200);

        var repositoryMock = new Mock<IRepository>();
        repositoryMock
            .SetupSequence(r => r.BpObtenerUno<Orden>(It.IsAny<Expression<Func<Orden, bool>>>()))
            .Returns(ordenAIniciar)
            .Returns(otraOrdenEnCamino);

        var tokenManagerMock = new Mock<Autorizacion.JwtManager.ITokenManager>();
        tokenManagerMock.Setup(t => t.ObtenerRepartidor(It.IsAny<IHttpContextAccessor>())).Returns(repartidor);

        var handler = new EstablecerEstadoRepartidorHandlerHandler(
            repositoryMock.Object, tokenManagerMock.Object, Mock.Of<IHttpContextAccessor>());

        var request = new EstablecerEstadoRepartidorRequest(ConstantesEstadoRepartidor.ENTREGANDO, IdOrden: 100);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(request, CancellationToken.None));
        Assert.Equal("Ya tienes una entrega en camino. Complétala antes de iniciar otra.", ex.Message);
    }

    [Fact]
    public async Task Handle_SinIdOrden_UsaComportamientoLegado()
    {
        var repartidor = TestEntityFactory.CrearRepartidor(id: 10, estado: ConstantesEstadoRepartidor.ASIGNADO);
        var ordenEnProcesamiento = TestEntityFactory.CrearOrden(id: 100);

        var repositoryMock = new Mock<IRepository>();
        repositoryMock
            .SetupSequence(r => r.BpObtenerUno<Orden>(It.IsAny<Expression<Func<Orden, bool>>>()))
            .Returns(ordenEnProcesamiento)
            .Returns((Orden?)null);

        var tokenManagerMock = new Mock<Autorizacion.JwtManager.ITokenManager>();
        tokenManagerMock.Setup(t => t.ObtenerRepartidor(It.IsAny<IHttpContextAccessor>())).Returns(repartidor);

        var handler = new EstablecerEstadoRepartidorHandlerHandler(
            repositoryMock.Object, tokenManagerMock.Object, Mock.Of<IHttpContextAccessor>());

        var request = new EstablecerEstadoRepartidorRequest(ConstantesEstadoRepartidor.ENTREGANDO);

        var response = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(ConstantesEstadoRepartidor.ENTREGANDO, response.EstadoRepartidor);
        ConstantesEstadoEnvio.EstadosEnvio.TryGetValue(ConstantesEstadoEnvio.ENCAMINO, out var idEnCamino);
        Assert.Equal(idEnCamino, ordenEnProcesamiento.IdEstadoEnvio);
    }
}
```

- [ ] **Step 2: Correr los tests y confirmar que fallan**

```bash
dotnet test BusinessPlaceServer.Tests/BusinessPlaceServer.Tests.csproj --filter EstablecerEstadoRepartidorHandlerHandlerTests
```

Expected: 3 de los 4 fallan (`ConIdOrdenValido`, `ConIdOrdenQueNoPertenece`, `ConOtraOrdenYaEnCamino` — el handler todavía ignora `IdOrden` y nunca valida "otra en camino"). `SinIdOrden` puede pasar de entrada porque coincide con el comportamiento actual.

- [ ] **Step 3: Implementar la lógica nueva**

Reemplazar el contenido completo de `PresentationLayer/Microservicios/Command/Agentes.Command.Api/Handlers/Repartidor/EstablecerEstadoRepartidorHandlerHandler.cs`:

```csharp
using Autorizacion.JwtManager;
using Constantes;
using Interfaces;
using MediatR;
using Mensajeria;
using Ventas;

namespace Agentes.Command.Handlers
{
    public class EstablecerEstadoRepartidorHandlerHandler(
        IRepository repository,
        ITokenManager tokenManager,
        IHttpContextAccessor httpContext)
        : IRequestHandler<EstablecerEstadoRepartidorRequest, EstablecerEstadoRepartidorResponse>
    {
        public async Task<EstablecerEstadoRepartidorResponse> Handle(EstablecerEstadoRepartidorRequest request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            var estadoEnvio = 1;
            var estadoOrden = 1;
            var repartidor = tokenManager.ObtenerRepartidor(httpContext);

            repartidor ??=
                    repository
                    .BpObtenerUno<Repartidor>(x => x.Id == request.IdRepartidor)!;

            var orden = request.IdOrden.HasValue
                ? repository.BpObtenerUno<Orden>(x => x.Id == request.IdOrden.Value &&
                                                       x.IdRepartidor == repartidor.Id &&
                                                       x.EsActivo)
                : repository.BpObtenerUno<Orden>(x => x.IdRepartidor == repartidor.Id &&
                                          x.EstadoOrden.Descripcion.Equals(ConstantesEstadoOrden.PROCESANDO) &&
                                          x.EsActivo);

            if (request.IdOrden.HasValue && orden is null)
                throw new InvalidOperationException($"La orden {request.IdOrden.Value} no pertenece a este repartidor o no existe.");

            if (orden is not null)
            {
                estadoEnvio = orden.IdEstadoEnvio;
                estadoOrden = orden.IdEstadoOrden;
            }

            switch (request.Estado)
            {
                case ConstantesEstadoRepartidor.DISPONIBLE://Repartidor se pone disponible
                    if (repartidor.Estado.Equals(ConstantesEstadoRepartidor.ENTREGANDO))//Cuando entrega la orden
                    {
                        ConstantesEstadoOrden.EstadosOrden.TryGetValue(ConstantesEstadoOrden.COMPLETADO, out estadoOrden);
                        ConstantesEstadoEnvio.EstadosEnvio.TryGetValue(ConstantesEstadoEnvio.ENTREGADO, out estadoEnvio);
                        if (orden is not null)
                        {
                            orden.IdEstadoOrden = estadoOrden;
                            orden.IdEstadoEnvio = estadoEnvio;
                        }
                    }
                    repartidor.Estado = request.Estado;
                    repartidor.EstaDisponible = true;
                    break;
                case ConstantesEstadoRepartidor.ENTREGANDO://Repartidor acepta la orden
                    var idOrdenActual = orden?.Id ?? 0;
                    var otraOrdenEnCamino =
                        repository
                        .BpObtenerUno<Orden>(x => x.IdRepartidor == repartidor.Id &&
                                                  x.Id != idOrdenActual &&
                                                  x.EstadoEnvio.Descripcion.Equals(ConstantesEstadoEnvio.ENCAMINO) &&
                                                  x.EsActivo);
                    if (otraOrdenEnCamino is not null)
                        throw new InvalidOperationException("Ya tienes una entrega en camino. Complétala antes de iniciar otra.");

                    ConstantesEstadoOrden.EstadosOrden.TryGetValue(ConstantesEstadoOrden.PROCESANDO, out estadoOrden);
                    ConstantesEstadoEnvio.EstadosEnvio.TryGetValue(ConstantesEstadoEnvio.ENCAMINO, out estadoEnvio);
                    if (orden is not null)
                    {
                        orden.IdEstadoOrden = estadoOrden;
                        orden.IdEstadoEnvio = estadoEnvio;
                    }
                    repartidor.Estado = request.Estado;
                    repartidor.EstaDisponible = false;
                    break;
                case ConstantesEstadoRepartidor.NO_DISPONIBLE://Repartidor se pone como estado no disponible
                    if (repartidor.Estado.Equals(ConstantesEstadoRepartidor.DISPONIBLE))//Solo si se encuentra disponible
                    {
                        repartidor.Estado = request.Estado;
                        repartidor.EstaDisponible = false;
                    }
                    break;
            }

            return new(repartidor.Id, repartidor.Estado);
        }
    }
}
```

- [ ] **Step 4: Correr los tests y confirmar que pasan**

```bash
dotnet test BusinessPlaceServer.Tests/BusinessPlaceServer.Tests.csproj --filter EstablecerEstadoRepartidorHandlerHandlerTests
```

Expected: `Passed! - Failed: 0, Passed: 4, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add PresentationLayer/Microservicios/Command/Agentes.Command.Api/Handlers/Repartidor/EstablecerEstadoRepartidorHandlerHandler.cs BusinessPlaceServer.Tests/EstablecerEstadoRepartidorHandlerHandlerTests.cs
git commit -m "Soportar IdOrden puntual y validar una sola entrega en camino"
```

---

## Task 5: Controller — traducir el rechazo de negocio a HTTP 409 con mensaje

Sin esto, el repartidor ve un error genérico ("Response status code does not indicate success") en vez del mensaje explicativo que el Task 4 ya lanza.

**Files:**
- Modify: `PresentationLayer/Microservicios/Command/Agentes.Command.Api/Controllers/RepartidorController.cs:26-31`

- [ ] **Step 1: Envolver la llamada al mediator**

Reemplazar (líneas 26-31):

```csharp
        public async Task<IActionResult> EstablecerEstadoRepartidor([FromBody] EstablecerEstadoRepartidorRequest request)
        {
            var respuesta = await mediator.Send(request);

            return Ok(respuesta);
        }
```

por:

```csharp
        public async Task<IActionResult> EstablecerEstadoRepartidor([FromBody] EstablecerEstadoRepartidorRequest request)
        {
            try
            {
                var respuesta = await mediator.Send(request);
                return Ok(respuesta);
            }
            catch (InvalidOperationException ex)
            {
                return new ContentResult
                {
                    Content = ex.Message,
                    StatusCode = 409,
                    ContentType = "text/plain"
                };
            }
        }
```

- [ ] **Step 2: Verificar que el microservicio compila**

```bash
dotnet build PresentationLayer/Microservicios/Command/Agentes.Command.Api/Agentes.Command.Api.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add PresentationLayer/Microservicios/Command/Agentes.Command.Api/Controllers/RepartidorController.cs
git commit -m "Devolver 409 con mensaje explicativo cuando falla EstablecerEstadoRepartidor"
```

---

## Task 6: `AsignarDeliveryHandler` — no pisar el estado de una entrega en curso (TDD)

**Files:**
- Modify: `PresentationLayer/Microservicios/Command/Ventas.Command.Api/Handlers/Orden/AsignarDeliveryHandler.cs`
- Create: `BusinessPlaceServer.Tests/AsignarDeliveryHandlerTests.cs`

- [ ] **Step 1: Escribir el test que falla**

Crear `BusinessPlaceServer.Tests/AsignarDeliveryHandlerTests.cs`:

```csharp
using System.Linq.Expressions;
using Constantes;
using Interfaces;
using Mensajeria;
using Moq;
using Ventas;
using Ventas.Command.Handlers;
using Xunit;

namespace BusinessPlaceServer.Tests;

public class AsignarDeliveryHandlerTests
{
    [Fact]
    public async Task Handle_CuandoRepartidorYaEstaEntregando_NoSobreescribeEstado()
    {
        var repartidor = TestEntityFactory.CrearRepartidor(id: 10, estado: ConstantesEstadoRepartidor.ENTREGANDO);
        var orden = TestEntityFactory.CrearOrden(id: 100);
        orden.Repartidor = repartidor;

        var repositoryMock = new Mock<IRepository>();
        repositoryMock
            .Setup(r => r.BpObtenerUno<Orden>(It.IsAny<Expression<Func<Orden, bool>>>()))
            .Returns(orden);

        var handler = new AsignarDeliveryHandler(repositoryMock.Object);
        var request = new AsignarDeliveryRequest(IdOrden: 100, IdRepartidor: 10);

        await handler.Handle(request, CancellationToken.None);

        Assert.Equal(ConstantesEstadoRepartidor.ENTREGANDO, repartidor.Estado);
    }

    [Fact]
    public async Task Handle_CuandoRepartidorNoEstaEntregando_AsignaEstadoAsignado()
    {
        var repartidor = TestEntityFactory.CrearRepartidor(id: 10, estado: ConstantesEstadoRepartidor.DISPONIBLE);
        var orden = TestEntityFactory.CrearOrden(id: 100);
        orden.Repartidor = repartidor;

        var repositoryMock = new Mock<IRepository>();
        repositoryMock
            .Setup(r => r.BpObtenerUno<Orden>(It.IsAny<Expression<Func<Orden, bool>>>()))
            .Returns(orden);

        var handler = new AsignarDeliveryHandler(repositoryMock.Object);
        var request = new AsignarDeliveryRequest(IdOrden: 100, IdRepartidor: 10);

        await handler.Handle(request, CancellationToken.None);

        Assert.Equal(ConstantesEstadoRepartidor.ASIGNADO, repartidor.Estado);
    }
}
```

- [ ] **Step 2: Correr los tests y confirmar que el primero falla**

```bash
dotnet test BusinessPlaceServer.Tests/BusinessPlaceServer.Tests.csproj --filter AsignarDeliveryHandlerTests
```

Expected: `Handle_CuandoRepartidorYaEstaEntregando_NoSobreescribeEstado` falla (hoy el handler pisa `Estado` incondicionalmente a `ASIGNADO`); `Handle_CuandoRepartidorNoEstaEntregando_AsignaEstadoAsignado` pasa (comportamiento actual ya es correcto en ese caso).

- [ ] **Step 3: Implementar el guard**

En `AsignarDeliveryHandler.cs`, reemplazar:

```csharp
                    x.Repartidor!.Estado = ConstantesEstadoRepartidor.ASIGNADO;
                    x.Repartidor!.EstaDisponible = false;
```

por:

```csharp
                    if (!x.Repartidor!.Estado.Equals(ConstantesEstadoRepartidor.ENTREGANDO))
                        x.Repartidor!.Estado = ConstantesEstadoRepartidor.ASIGNADO;
                    x.Repartidor!.EstaDisponible = false;
```

- [ ] **Step 4: Correr los tests y confirmar que ambos pasan**

```bash
dotnet test BusinessPlaceServer.Tests/BusinessPlaceServer.Tests.csproj --filter AsignarDeliveryHandlerTests
```

Expected: `Passed! - Failed: 0, Passed: 2, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add PresentationLayer/Microservicios/Command/Ventas.Command.Api/Handlers/Orden/AsignarDeliveryHandler.cs BusinessPlaceServer.Tests/AsignarDeliveryHandlerTests.cs
git commit -m "No pisar Repartidor.Estado al asignar una orden si ya está entregando otra"
```

---

## Task 7: Exponer `EstadoEnvio` en `EntregaResumen` (sin test automatizado — ver Task 12)

Cambio aditivo de una línea de lógica; el handler completo (`ObtenerEntregasHandler`) atraviesa una cadena profunda de propiedades de navegación (`Orden.OrdenesProductos`, `.Moneda`, `.Cliente`, `.Entrega`, `VendedorProducto.Producto/.Vendedor`, `Direccion`) que haría desproporcionado armar un test unitario solo para este campo — se verifica manualmente en el Task 12.

**Files:**
- Modify: `InfraestructureLayer/Mensajeria/Ventas/EntregaMensajes.cs`
- Modify: `PresentationLayer/Microservicios/Query/Ventas.Query.Api/Handlers/Entrega/ObtenerEntregasHandler.cs`

- [ ] **Step 1: Agregar el campo a `EntregaResumen`**

En `EntregaMensajes.cs`, reemplazar:

```csharp
    public class EntregaResumen(ProductoEntregaResumen[] productos,
                                string codigoMoneda,
                                string metodoPago,
                                VendedorEntregaResumen[] vendedores,
                                decimal costoEnvio,
                                decimal subTotal,
                                decimal total,
                                string telefonoCliente,
                                int id,
                                string nombreCliente,
                                string imagenCliente,
                                string direccionEntrega,
                                string coordenadas)
    {
        public ProductoEntregaResumen[] Productos { get; } = productos;
        public string CodigoMoneda { get; } = codigoMoneda;
        public string MetodoPago { get; } = metodoPago;
        public VendedorEntregaResumen[] Vendedores { get; } = vendedores;
        public decimal CostoEnvio { get; } = costoEnvio;
        public decimal SubTotal { get; } = subTotal;
        public decimal Total { get; } = total;
        public string TelefonoCliente { get; } = telefonoCliente;
        public int Id { get; } = id;
        public string NombreCliente { get; } = nombreCliente;
        public string ImagenCliente { get; } = imagenCliente;
        public string DireccionEntrega { get; } = direccionEntrega;
        public string Coordenadas { get; } = coordenadas;
    }
```

por:

```csharp
    public class EntregaResumen(ProductoEntregaResumen[] productos,
                                string codigoMoneda,
                                string metodoPago,
                                VendedorEntregaResumen[] vendedores,
                                decimal costoEnvio,
                                decimal subTotal,
                                decimal total,
                                string telefonoCliente,
                                int id,
                                string nombreCliente,
                                string imagenCliente,
                                string direccionEntrega,
                                string coordenadas,
                                string estadoEnvio)
    {
        public ProductoEntregaResumen[] Productos { get; } = productos;
        public string CodigoMoneda { get; } = codigoMoneda;
        public string MetodoPago { get; } = metodoPago;
        public VendedorEntregaResumen[] Vendedores { get; } = vendedores;
        public decimal CostoEnvio { get; } = costoEnvio;
        public decimal SubTotal { get; } = subTotal;
        public decimal Total { get; } = total;
        public string TelefonoCliente { get; } = telefonoCliente;
        public int Id { get; } = id;
        public string NombreCliente { get; } = nombreCliente;
        public string ImagenCliente { get; } = imagenCliente;
        public string DireccionEntrega { get; } = direccionEntrega;
        public string Coordenadas { get; } = coordenadas;
        public string EstadoEnvio { get; } = estadoEnvio;
    }
```

- [ ] **Step 2: Pasar el valor real en `ObtenerEntregasHandler`**

En `ObtenerEntregasHandler.cs`, reemplazar la construcción de `entregaResumen`:

```csharp
                var entregaResumen = new EntregaResumen(produtosEntregaResumen,
                                                        orden.Moneda.Codigo,
                                                        orden.CodigoMetodoPago,
                                                        vendedoresEntregaResumen,
                                                        orden.Entrega!.CostoEnvio,
                                                        orden.Subtotal,
                                                        orden.Total + orden.Entrega!.CostoEnvio,
                                                        direccionEntrega?.Telefono ?? string.Empty,
                                                        orden.Id,
                                                        orden.Cliente.Nombre,
                                                        orden.Cliente.Apellido,
                                                        direccionEntrega?.CodigoPostal ?? string.Empty,
                                                        $"{direccionEntrega?.Latitud ?? string.Empty},{direccionEntrega?.Longitud ?? string.Empty}");
```

por:

```csharp
                var entregaResumen = new EntregaResumen(produtosEntregaResumen,
                                                        orden.Moneda.Codigo,
                                                        orden.CodigoMetodoPago,
                                                        vendedoresEntregaResumen,
                                                        orden.Entrega!.CostoEnvio,
                                                        orden.Subtotal,
                                                        orden.Total + orden.Entrega!.CostoEnvio,
                                                        direccionEntrega?.Telefono ?? string.Empty,
                                                        orden.Id,
                                                        orden.Cliente.Nombre,
                                                        orden.Cliente.Apellido,
                                                        direccionEntrega?.CodigoPostal ?? string.Empty,
                                                        $"{direccionEntrega?.Latitud ?? string.Empty},{direccionEntrega?.Longitud ?? string.Empty}",
                                                        orden.EstadoEnvio.Descripcion);
```

- [ ] **Step 3: Verificar que ambos proyectos compilan**

```bash
dotnet build InfraestructureLayer/Mensajeria/Mensajeria.csproj
dotnet build PresentationLayer/Microservicios/Query/Ventas.Query.Api/Ventas.Query.Api.csproj
```

Expected: `Build succeeded.` en ambos.

- [ ] **Step 4: Commit**

```bash
git add InfraestructureLayer/Mensajeria/Ventas/EntregaMensajes.cs PresentationLayer/Microservicios/Query/Ventas.Query.Api/Handlers/Entrega/ObtenerEntregasHandler.cs
git commit -m "Exponer EstadoEnvio real en la respuesta de entregas asignadas"
```

---

## Task 8: Reconstruir y copiar `Mensajeria.dll` a DhahabiDelivery

`Mensajeria.dll` no es un paquete NuGet ni un `ProjectReference` — es un binario que se compila en `BusinessPlaceServer` y se copia a mano (ver `WORKSPACE.md`). Sin este paso, el cliente no puede ver `IdOrden` en el request ni `EstadoEnvio` en la respuesta aunque el código C# ya compile en cada repo por separado.

**Files:**
- Build output: `BusinessPlaceServer/InfraestructureLayer/Mensajeria/bin/Release/net8.0/Mensajeria.dll`
- Copy to: `DhahabiDelivery/DhahabiDelivery/Configuration/Mensajeria.dll`

- [ ] **Step 1: Compilar Mensajeria en Release**

Desde `BusinessPlaceServer/`:

```bash
dotnet build InfraestructureLayer/Mensajeria/Mensajeria.csproj -c Release
```

Expected: `Build succeeded.`

- [ ] **Step 2: Copiar el DLL al cliente**

```bash
cp InfraestructureLayer/Mensajeria/bin/Release/net8.0/Mensajeria.dll /home/hallen/Dhahabi/DhahabiDelivery/DhahabiDelivery/Configuration/Mensajeria.dll
```

- [ ] **Step 3: Verificar que DhahabiDelivery sigue compilando con el DLL nuevo**

Desde `DhahabiDelivery/`:

```bash
dotnet build DhahabiDelivery/DhahabiDelivery.csproj -f net9.0-android35.0
```

Expected: `Build succeeded.` (Este paso solo confirma que el binario nuevo no rompe el build actual — los usos de `IdOrden`/`EstadoEnvio` se agregan en las tareas siguientes.)

- [ ] **Step 4: Commit (en DhahabiDelivery)**

```bash
cd /home/hallen/Dhahabi/DhahabiDelivery
git add DhahabiDelivery/Configuration/Mensajeria.dll
git commit -m "Actualizar Mensajeria.dll: IdOrden en EstablecerEstadoRepartidorRequest, EstadoEnvio en EntregaResumen"
```

---

## Task 9: `HttpHelper` — no descartar el mensaje de error del backend

Sin esto, `response.EnsureSuccessStatusCode()` descarta el cuerpo de la respuesta 409 del Task 5, y el repartidor ve un mensaje genérico en vez de "Ya tienes una entrega en camino...".

**Files:**
- Modify: `DhahabiDelivery/Modules/Shared/Services/HttpHelper.cs:49-53`

- [ ] **Step 1: Leer el cuerpo del error antes de lanzar la excepción**

Reemplazar (dentro de `MakeHttpRequestAsync<TRequest, TResponse>`):

```csharp
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        if (responseString == null) throw new InvalidOperationException("El contenido de la respuesta es nulo.");

        return responseString;
```

por:

```csharp
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(errorBody) ? $"Error {(int)response.StatusCode} ({response.StatusCode})" : errorBody,
                null,
                response.StatusCode);
        }

        var responseString = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        if (responseString == null) throw new InvalidOperationException("El contenido de la respuesta es nulo.");

        return responseString;
```

- [ ] **Step 2: Verificar que el proyecto compila**

```bash
dotnet build DhahabiDelivery/DhahabiDelivery.csproj -f net9.0-android35.0
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add DhahabiDelivery/Modules/Shared/Services/HttpHelper.cs
git commit -m "Propagar el mensaje de error del backend en vez de descartarlo"
```

---

## Task 10: `IRepartidorService`/`RepartidorService`/`RepartidorServiceMock` — mandar `IdOrden`

**Files:**
- Modify: `DhahabiDelivery/Modules/Entregas/Services/RepartidorService-interface.cs`
- Modify: `DhahabiDelivery/Modules/Entregas/Services/RepartidorService.cs`
- Modify: `DhahabiDelivery/Modules/Entregas/Services/RepartidorServiceMock.cs`

- [ ] **Step 1: Cambiar la firma de `FinalizarEntrega` en la interfaz**

En `RepartidorService-interface.cs`, reemplazar:

```csharp
public interface IRepartidorService
{
    Task<string> IniciarEntrega(EntregaResumen ordenAsignada);
    Task<string> FinalizarEntrega();
    Task<string> ObtenerEstadoRepartidor();
    Task<string> EstablecerEstadoRepartidor(string estado);
    Task UpdateLocation(LatLngLiteral deliveryLocation);
}
```

por:

```csharp
public interface IRepartidorService
{
    Task<string> IniciarEntrega(EntregaResumen ordenAsignada);
    Task<string> FinalizarEntrega(int idOrden);
    Task<string> ObtenerEstadoRepartidor();
    Task<string> EstablecerEstadoRepartidor(string estado);
    Task UpdateLocation(LatLngLiteral deliveryLocation);
}
```

- [ ] **Step 2: Implementar en `RepartidorService`**

En `RepartidorService.cs`, reemplazar:

```csharp
    public async Task<string> IniciarEntrega(EntregaResumen ordenAsignada)
    {
        var req = new EstablecerEstadoRepartidorRequest(ConstantesEstadoRepartidor.ENTREGANDO);
        var config =
            new HttpHelper.HttpHelperConfig(Apis.AgentesCommand.Name, Apis.AgentesCommand.EstablecerEstadoRepartidor);
        var res = await httpHelper
            .MakeHttpRequestAsync<EstablecerEstadoRepartidorRequest, EstablecerEstadoRepartidorResponse>(req, config);
        return res.EstadoRepartidor;
    }

    public async Task<string> FinalizarEntrega()
    {
        var req = new EstablecerEstadoRepartidorRequest(ConstantesEstadoRepartidor.DISPONIBLE);
        var config =
            new HttpHelper.HttpHelperConfig(Apis.AgentesCommand.Name, Apis.AgentesCommand.EstablecerEstadoRepartidor);
        var res = await httpHelper
            .MakeHttpRequestAsync<EstablecerEstadoRepartidorRequest, EstablecerEstadoRepartidorResponse>(req, config);
        return res.EstadoRepartidor;
    }
```

por:

```csharp
    public async Task<string> IniciarEntrega(EntregaResumen ordenAsignada)
    {
        var req = new EstablecerEstadoRepartidorRequest(ConstantesEstadoRepartidor.ENTREGANDO, IdOrden: ordenAsignada.Id);
        var config =
            new HttpHelper.HttpHelperConfig(Apis.AgentesCommand.Name, Apis.AgentesCommand.EstablecerEstadoRepartidor);
        var res = await httpHelper
            .MakeHttpRequestAsync<EstablecerEstadoRepartidorRequest, EstablecerEstadoRepartidorResponse>(req, config);
        return res.EstadoRepartidor;
    }

    public async Task<string> FinalizarEntrega(int idOrden)
    {
        var req = new EstablecerEstadoRepartidorRequest(ConstantesEstadoRepartidor.DISPONIBLE, IdOrden: idOrden);
        var config =
            new HttpHelper.HttpHelperConfig(Apis.AgentesCommand.Name, Apis.AgentesCommand.EstablecerEstadoRepartidor);
        var res = await httpHelper
            .MakeHttpRequestAsync<EstablecerEstadoRepartidorRequest, EstablecerEstadoRepartidorResponse>(req, config);
        return res.EstadoRepartidor;
    }
```

- [ ] **Step 3: Actualizar el mock**

En `RepartidorServiceMock.cs`, reemplazar:

```csharp
    public async Task<string> FinalizarEntrega()
    {
        await Task.Delay(1000);
        _estado = ConstantesEstadoRepartidor.DISPONIBLE;
        return ConstantesEstadoRepartidor.DISPONIBLE;
    }
```

por:

```csharp
    public async Task<string> FinalizarEntrega(int idOrden)
    {
        await Task.Delay(1000);
        _estado = ConstantesEstadoRepartidor.DISPONIBLE;
        return ConstantesEstadoRepartidor.DISPONIBLE;
    }
```

- [ ] **Step 4: Verificar que el proyecto compila**

```bash
dotnet build DhahabiDelivery/DhahabiDelivery.csproj -f net9.0-android35.0
```

Expected: `Build succeeded.` (Este build va a fallar hasta completar el Task 11, porque `EntregasViewModel.FinalizarEntrega()` todavía llama a `repartidorService.FinalizarEntrega()` sin argumento — es esperado, se corrige en el próximo task.)

- [ ] **Step 5: Commit**

```bash
git add DhahabiDelivery/Modules/Entregas/Services/RepartidorService-interface.cs DhahabiDelivery/Modules/Entregas/Services/RepartidorService.cs DhahabiDelivery/Modules/Entregas/Services/RepartidorServiceMock.cs
git commit -m "Mandar IdOrden puntual en Iniciar/FinalizarEntrega"
```

---

## Task 11: `EntregasViewModel` — pasar `IdOrden` y bloquear inicio de una segunda entrega en camino

**Files:**
- Modify: `DhahabiDelivery/Modules/Entregas/ViewModels/EntregasViewModel.cs`
- Modify: `DhahabiDelivery/Modules/Entregas/ConstantesEntrega.cs`

- [ ] **Step 1: Agregar la constante `EnCamino` del lado del cliente**

En `ConstantesEntrega.cs`, agregar al final del archivo:

```csharp

public static class ConstantesEstadoEnvio
{
    public const string ENCAMINO = "En camino";
}
```

(El valor debe coincidir textualmente con `ConstantesEstadoEnvio.ENCAMINO` del servidor — `"En camino"` — porque viaja como string plano en `EntregaResumen.EstadoEnvio`, no hay un enum compartido entre los dos repos.)

- [ ] **Step 2: Pasar el `IdOrden` al finalizar y agregar el chequeo de "otra en camino"**

En `EntregasViewModel.cs`, reemplazar:

```csharp
    public async Task FinalizarEntrega()
    {
        // Obtener el estado actual del repartidor
        var estadoActual = await repartidorService.ObtenerEstadoRepartidor();

        // Verificar si el repartidor está en estado "E"
        if (estadoActual != ConstantesEstadoRepartidor.ENTREGANDO &&
            estadoActual != ConstantesEstadoRepartidor.DISPONIBLE)
            // No permitir finalizar la entrega si no está en estado "E"
            return;

        var state = await repartidorService.FinalizarEntrega();
        EntregaSeleccionada = null;
        EntregasAsignadas = [];
        storageService.Remove(ConstantesEstadoRepartidor.ORDER_ASIGNED_KEY);
        authService.SetDeliveryStateAsync(state);
        State = state;

        // Actualizar el estado en el servicio de ubicación
        await locationService.UpdateDeliveryStateAsync(state);
        Console.WriteLine($"✅ Entrega finalizada - Estado: {state}");
    }
```

por:

```csharp
    public async Task FinalizarEntrega()
    {
        // Obtener el estado actual del repartidor
        var estadoActual = await repartidorService.ObtenerEstadoRepartidor();

        // Verificar si el repartidor está en estado "E"
        if (estadoActual != ConstantesEstadoRepartidor.ENTREGANDO &&
            estadoActual != ConstantesEstadoRepartidor.DISPONIBLE)
            // No permitir finalizar la entrega si no está en estado "E"
            return;

        if (EntregaSeleccionada == null) return;

        var state = await repartidorService.FinalizarEntrega(EntregaSeleccionada.Id);
        EntregaSeleccionada = null;
        EntregasAsignadas = [];
        storageService.Remove(ConstantesEstadoRepartidor.ORDER_ASIGNED_KEY);
        authService.SetDeliveryStateAsync(state);
        State = state;

        // Actualizar el estado en el servicio de ubicación
        await locationService.UpdateDeliveryStateAsync(state);
        Console.WriteLine($"✅ Entrega finalizada - Estado: {state}");
    }

    // Verdadero si alguna otra entrega asignada (distinta a la que se le pasa) ya está en camino.
    public bool TieneOtraEntregaEnCamino(EntregaResumen entrega) =>
        EntregasAsignadas.Any(e => e.Id != entrega.Id && e.EstadoEnvio == ConstantesEstadoEnvio.ENCAMINO);
```

- [ ] **Step 3: Verificar que el proyecto compila**

```bash
dotnet build DhahabiDelivery/DhahabiDelivery.csproj -f net9.0-android35.0
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add DhahabiDelivery/Modules/Entregas/ViewModels/EntregasViewModel.cs DhahabiDelivery/Modules/Entregas/ConstantesEntrega.cs
git commit -m "ViewModel: mandar IdOrden al finalizar y exponer chequeo de otra entrega en camino"
```

---

## Task 12: `MapSection.razor` — usar el chequeo antes de iniciar

**Files:**
- Modify: `DhahabiDelivery/Modules/Entregas/sections/MapSection.razor`

- [ ] **Step 1: Bloquear "Iniciar" con el mismo diálogo de error que ya existe**

En `MapSection.razor`, reemplazar el método `Iniciar()`:

```csharp
    private async Task Iniciar()
    {
        if (_loadingButtonState == LoadingButton.State.Loading) return;
        try
        {
            _error = false;
            _loadingButtonState = LoadingButton.State.Loading;
            var estado = await ViewModel.ObtenerEstadoRepartidorAsync();
            if (estado == ConstantesEstadoRepartidor.ENTREGANDO)
            {
                await ShowFinalizarEntregaDialog();
                return;
            }

            if (estado != ConstantesEstadoRepartidor.ASIGNADO && estado != ConstantesEstadoRepartidor.DISPONIBLE) return;
            await ViewModel.IniciarEntrega();
            _loadingButtonState = LoadingButton.State.Success;
        }
        catch (Exception e)
        {
            _error = true;
            _loadingButtonState = LoadingButton.State.Error;
            _errorMessage = e.Message;
            _dialogTitle = "Ha ocurrido un error";
            await _dialogRef.Open();
        }
        finally
        {
            _loadingButtonState = LoadingButton.State.Normal;
            StateHasChanged();
        }
    }
```

por:

```csharp
    private async Task Iniciar()
    {
        if (_loadingButtonState == LoadingButton.State.Loading) return;
        try
        {
            _error = false;
            _loadingButtonState = LoadingButton.State.Loading;
            var estado = await ViewModel.ObtenerEstadoRepartidorAsync();
            if (estado == ConstantesEstadoRepartidor.ENTREGANDO)
            {
                await ShowFinalizarEntregaDialog();
                return;
            }

            if (estado != ConstantesEstadoRepartidor.ASIGNADO && estado != ConstantesEstadoRepartidor.DISPONIBLE) return;

            if (ViewModel.EntregaSeleccionada != null && ViewModel.TieneOtraEntregaEnCamino(ViewModel.EntregaSeleccionada))
            {
                _error = true;
                _errorMessage = "Ya tienes una entrega en camino. Complétala antes de iniciar otra.";
                _dialogTitle = "Ha ocurrido un error";
                await _dialogRef.Open();
                return;
            }

            await ViewModel.IniciarEntrega();
            _loadingButtonState = LoadingButton.State.Success;
        }
        catch (Exception e)
        {
            _error = true;
            _loadingButtonState = LoadingButton.State.Error;
            _errorMessage = e.Message;
            _dialogTitle = "Ha ocurrido un error";
            await _dialogRef.Open();
        }
        finally
        {
            _loadingButtonState = LoadingButton.State.Normal;
            StateHasChanged();
        }
    }
```

- [ ] **Step 2: Verificar que el proyecto compila**

```bash
dotnet build DhahabiDelivery/DhahabiDelivery.csproj -f net9.0-android35.0
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add DhahabiDelivery/Modules/Entregas/sections/MapSection.razor
git commit -m "Bloquear Iniciar cuando ya hay otra entrega en camino"
```

---

## Task 13: `EntregasItem.razor` — badge de estado real

**Files:**
- Modify: `DhahabiDelivery/Modules/Entregas/Components/EntregasItem.razor:8`

- [ ] **Step 1: Reemplazar el texto hardcodeado**

Reemplazar:

```razor
            <span class="text-[11px] w-fit rounded-full text-white px-2  bg-orange-400">Pendiente</span>
```

por:

```razor
            <span class="text-[11px] w-fit rounded-full text-white px-2  bg-orange-400">@Item.EstadoEnvio</span>
```

- [ ] **Step 2: Verificar que el proyecto compila**

```bash
dotnet build DhahabiDelivery/DhahabiDelivery.csproj -f net9.0-android35.0
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add DhahabiDelivery/Modules/Entregas/Components/EntregasItem.razor
git commit -m "Mostrar el estado real de envío en la lista de entregas en vez de un texto fijo"
```

---

## Task 14: Verificación manual end-to-end

Cubre lo que no tiene test automatizado (Task 7) y confirma el flujo completo con el stack de test que ya existe en `BusinessPlaceServer` (`build-test.sh`).

- [ ] **Step 1: Levantar el stack de test del servidor**

Desde `BusinessPlaceServer/`:

```bash
./build-test.sh
```

Expected: los servicios `agentescommandtest`, `ventascommandtest`, `ventasquerytest` (entre otros) quedan arriba.

- [ ] **Step 2: Verificar `EstadoEnvio` en la respuesta de `ObtenerEntregasResumenLista`**

Con un usuario repartidor válido (token de prueba existente en el entorno), llamar al endpoint de query de entregas y confirmar que cada item del array `Entregas` incluye el campo `EstadoEnvio` con un valor no vacío (`"Pendiente"`, `"En camino"`, etc. según el estado real de cada orden en la base de datos de test).

- [ ] **Step 3: Probar el flujo de dos órdenes en el móvil**

Con dos órdenes asignadas al mismo repartidor de prueba (vía asignación manual como admin):
1. Entrar al detalle de la orden A y tocar "Iniciar" → debe pasar a `EnCamino`.
2. Volver a Home, entrar al detalle de la orden B y tocar "Iniciar" → debe mostrar el diálogo de error "Ya tienes una entrega en camino. Complétala antes de iniciar otra." sin llamar al backend.
3. Volver a la orden A, finalizarla → debe pasar a `Entregado`.
4. Entrar a la orden B y tocar "Iniciar" → debe funcionar normalmente.

- [ ] **Step 4: Confirmar que un cliente sin `IdOrden` (simulando una versión vieja de la app) sigue funcionando**

Llamar a `EstablecerEstadoRepartidor` mandando el payload sin el campo `IdOrden` (comportamiento legado) contra un repartidor con una sola orden asignada — debe comportarse exactamente igual que antes del cambio.

---

## Task 15: Rollout — EN PAUSA, pendiente de la estrategia de rama `deploy`/CI

**Esta tarea está pausada.** La estrategia de branching/CI de `BusinessPlaceServer` (rama `deploy` + auto-deploy al pushear ahí, relacionado con el punto 5 del backlog en WORKFLOW.md) todavía no está diseñada — merece su propia conversación, no una decisión de paso en medio de esta feature. No ejecutar los pasos de abajo hasta que esa estrategia esté definida y el usuario confirme explícitamente.

Decisiones ya tomadas para cuando se retome:
- El código de esta feature queda en `fix/delivery-app` (ya rebasada sobre `origin/dev-stripe`, la rama realmente activa — ver nota más abajo) sin pushear por ahora.
- Cuando se defina la rama `deploy` y su CI, el push debe ir **tanto a `fix/delivery-app` como a `deploy`** (no solo a una), para que el cambio quede listo para desplegarse automáticamente en cuanto el CI esté configurado.
- El PR de revisión (si corresponde) probablemente ya no apunte a `dev` sino a lo que se decida como rama de integración real — a confirmar en esa conversación.

- [ ] **Step 1: Confirmar el estado de la rama**

```bash
cd /home/hallen/Dhahabi/BusinessPlaceServer
git log origin/dev-stripe..fix/delivery-app --oneline
```

Expected: la lista de todos los commits de las Tasks 1-7 (los de este repo).

- [ ] **Step 2: Push (pedir confirmación antes de este paso)**

```bash
git push -u origin fix/delivery-app
git push origin fix/delivery-app:deploy   # o el nombre final que se acuerde para la rama de deploy
```

- [ ] **Step 3: Abrir PR de revisión (base branch a confirmar en la conversación de branching/CI, ya no necesariamente `dev`)**

```bash
gh pr create --base <RAMA_A_CONFIRMAR> --head fix/delivery-app --title "Multi-orden: estado de entrega por orden en vez de por repartidor" --body "$(cat <<'EOF'
## Resumen
- `Orden.IdEstadoEnvio` pasa a ser la fuente de verdad de "¿qué está pasando con esta entrega?" en vez de `Repartidor.Estado` (que mezclaba disponibilidad + asignación + en-camino en un solo campo).
- `EstablecerEstadoRepartidorRequest` gana un `IdOrden` opcional — compatible con clientes viejos que no lo mandan.
- Nueva validación: un repartidor no puede tener dos órdenes `EnCamino` a la vez.
- `AsignarDeliveryHandler` ya no pisa `Repartidor.Estado` si el repartidor ya está entregando otra orden.
- Primer proyecto de tests del repo (xUnit + Moq) — cubre los dos handlers tocados.

## Test plan
- [x] Tests unitarios nuevos pasan (`dotnet test`)
- [x] Verificación manual con el stack `build-test.sh` (dos órdenes simultáneas, cliente legado sin IdOrden)
- [ ] Revisión del mantenedor de BusinessPlaceServer
EOF
)"
```

- [ ] **Step 4: Push de los cambios en DhahabiDelivery (repo del que el usuario sí es mantenedor)**

```bash
cd /home/hallen/Dhahabi/DhahabiDelivery
git push
```
