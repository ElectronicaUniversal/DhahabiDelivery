# DhahabiDelivery

> Ver también: [../WORKSPACE.md](../WORKSPACE.md) y [ARCHITECTURE.md](ARCHITECTURE.md).

## Qué es esto

La app del repartidor de Dhahabi: un único proyecto **.NET MAUI** (Android/iOS/MacCatalyst) que consume el rol `Repartidor` de BusinessPlaceServer para recibir entregas asignadas, reportar posición GPS y marcar entregas completadas (con escaneo de código de barras/QR).

**⚠️ Este proyecto todavía no tiene repositorio git.** Fue scaffoldeado copiando a mano el patrón de `BusinessPlaceClient/FrontentHybrid` + `FrontentCompartido` (ver [Estado del proyecto](#estado-del-proyecto) y [../WORKSPACE.md](../WORKSPACE.md#plan-de-puesta-en-marcha-para-dhahabidelivery) para el plan antes de crear el repo).

## Arranque rápido

### Prerrequisitos

- .NET 8 SDK (no hay `global.json` que lo pin explícitamente — a diferencia de BusinessPlaceClient)
- Workloads MAUI (`dotnet workload install maui`)
- Node.js (solo para recompilar el CSS de Tailwind, ver más abajo)

### Instalación

```bash
cd DhahabiDelivery
dotnet build DhahabiDelivery/DhahabiDelivery.csproj -f net8.0-android34.0   # o -ios / -maccatalyst
```

### CSS (Tailwind)

```bash
cd DhahabiDelivery/DhahabiDelivery
npm install
npx tailwindcss -i ./wwwroot/css/app.css -o ./wwwroot/css/app.min.css --minify
```

⚠️ `package.json` fija `tailwindcss ^3.3.3`, pero el CSS compilado que está en el repo (`app.min.css`) tiene el banner de la v4.0.12 — el `package.json` está desactualizado respecto a lo que realmente se usó para generar el CSS actual. Si corres el comando de arriba con el `node_modules` instalado tal cual está, es probable que el output no coincida byte a byte con `app.min.css`.

### Verificar que funciona

Al levantar en un emulador/dispositivo Android, deberías ver la pantalla de login (`PaginaLogin`). No hay Google Sign-In acá (a diferencia de BusinessPlaceClient) — solo email/contraseña.

## Estructura del proyecto

```
DhahabiDelivery/
├── DhahabiDelivery.sln
├── dhahabi-delivery.keystore       # Firma Android — actualmente NO wireada (comentada en el .csproj)
└── DhahabiDelivery/                 # El único proyecto de la solución
    ├── Modules/
    │   ├── Auth/                    # Login, registro, recuperación (email/contraseña, sin Google)
    │   ├── Entregas/                # El módulo principal: mapa, detalle de entrega, servicios
    │   ├── Scanner/                  # Escaneo de código de barras/QR (ZXing.Net.Maui)
    │   ├── Layout/                   # Layout principal, bottom bar
    │   ├── Shared/                   # Componentes cross-cutting, LocationService, HttpHelper
    │   └── Usuario/                  # Perfil, estado de disponibilidad del repartidor
    ├── Configuration/
    │   ├── AppSettings.json / .cs    # URLs del backend
    │   ├── Apis.cs                    # Rutas por microservicio
    │   ├── Mensajeria.dll             # Contratos compartidos con el Server (copiado a mano)
    │   └── Transition/                # Router de transiciones — copiado de BusinessPlaceClient
    ├── Documentacion/
    │   └── ServicioLocalizacion.md    # Documento completo del sistema de tracking GPS
    ├── Platforms/{Android,iOS,MacCatalyst,Windows,Tizen}/
    ├── package.json, tailwind.config.js, node_modules/
    └── CameraPage.xaml(.cs)           # Suelto en la raíz, sin modularizar todavía
```

### Carpetas clave explicadas

**Modules/Entregas/** — el corazón de la app: `MapSection.razor` (Google Maps vía `BlazorGoogleMaps`), `EntregasService`/`RepartidorService` (y sus versiones `*Mock` para desarrollo sin backend), `EntregaDetailSection`, `EntregaCompletadaSection`.

**Configuration/Mensajeria.dll** — la misma pieza que en BusinessPlaceClient: DTOs de request/response generados en el Server. Acá se referencia de forma independiente (no vía BusinessPlaceClient), así que hay **dos copias separadas** de mantener sincronizadas — ver [../WORKSPACE.md](../WORKSPACE.md).

## Conceptos clave

### El sistema de tracking GPS (`LocationService`)

Documentado en detalle en `Documentacion/ServicioLocalizacion.md`. Reporta la posición del repartidor al backend con una frecuencia que depende de su estado:

- **No disponible**: no envía posición.
- **Disponible**: cada 30 segundos.
- **Entregando**: cada 5 segundos.

Implementado con un foreground service de Android (`LocationForegroundServiceFixed`, notificación persistente) para poder seguir trackeando con la app en segundo plano.

**Por qué importa:** cualquier cambio a los estados del repartidor (`NO_DISPONIBLE`/`DISPONIBLE`/`ASIGNADO`/`ENTREGANDO`) tiene que mantenerse sincronizado con las mismas constantes en BusinessPlaceServer (`ConstantesEstadoRepartidor.cs`).

### Wiring de HTTP incompleto

`Configuration/ServiceConfiguration.cs` solo registra `HttpClient` para 6 de los ~10 backends configurados en `AppSettings.json` (`AuthenticationQuery/Command`, `VentasQuery`, `AgentesQuery/Command`, `GeneralesQuery`). Las URLs de Pagos, Catálogo, Promociones y `VentasCommand` están en `AppSettings.json` pero **no tienen `HttpClient` registrado** — si necesitas consumir esos endpoints, hay que agregar la línea de `AddHttpClient` primero.

## Estado del proyecto

Señales concretas de que este proyecto está menos maduro que los otros dos:

- **Sin repo git** — todo el trabajo hasta ahora es local, sin historial.
- **`.gitignore` incompleto**: existe (`DhahabiDelivery/.gitignore`) pero solo ignora `.qodo/` — falta `bin/`, `obj/`, `node_modules/`, etc. Si se hace `git init` tal cual está hoy, hay que arreglar esto **antes** del primer commit.
- **Firma Android sin activar**: el bloque de `AndroidSigningKeyStore`/`AndroidSigningStorePass` en el `.csproj` está comentado, con un path hardcodeado a una máquina Windows específica (`C:\Users\Adrian\...`) y una contraseña en texto plano dentro del comentario.
- **Namespace con copy-paste sin terminar**: `Modules/Shared/Services/ImageService-interface.cs` declara `namespace FrontentCompartido.Modules.Shared.Services;` en vez de `DhahabiDelivery...` — quedó así de cuando se copió el archivo desde BusinessPlaceClient. Compila igual porque otros archivos hacen `using FrontentCompartido.Modules.Shared.Services;`, pero es un cabo suelto a limpiar.
- **Icono de Google sin usar**: `Modules/Shared/Icons/IconoGoogle.razor` existe pero no está conectado a ningún botón de login — remanente de la copia, no una feature a medio hacer intencional.
- **Carpetas vacías**: `Services/` (al lado de `Modules/`) y `Modules/Shared/Pages/` no tienen archivos.
- **Sin CI** (no hay repo, así que tampoco hay dónde correrlo).

Ninguno de estos puntos es urgente por sí solo, pero conviene resolverlos (al menos el `.gitignore` y la limpieza del namespace) antes de abrir el repositorio — ver el plan en [../WORKSPACE.md](../WORKSPACE.md#plan-de-puesta-en-marcha-para-dhahabidelivery).

## Tareas comunes

### Agregar un endpoint del backend que todavía no tiene `HttpClient`

1. Confirmar que la URL ya está en `Configuration/AppSettings.json` (si no, agregarla — debe coincidir con lo que expone BusinessPlaceServer).
2. Agregar la línea correspondiente en `Configuration/ServiceConfiguration.cs → AddHttpClients`.
3. Agregar la constante de ruta a `Configuration/Apis.cs`.

### Agregar una pantalla nueva al módulo Entregas

1. Crear el `.razor` en `Modules/Entregas/Pages/` (o `Sections/` si es una sub-sección de una pantalla existente, siguiendo el patrón de `MainSection`/`MapSection`/`EntregaDetailSection`).
2. Si necesita datos nuevos del backend, agregar el método a `EntregasService` (y su contraparte en `EntregasServiceMock` para poder probar sin backend).

## Configuración

| Clave (`Configuration/AppSettings.json`) | Valor | Con `HttpClient` registrado |
|---|---|---|
| `ImageServer` | `https://cdn.dhahabi.ae/images/` | — |
| `AuthenticationQuery` / `AuthenticationCommand` | `https://api.dhahabi.ae/autenticacion{query,command}/` | ✅ |
| `VentasQuery` | `https://api.dhahabi.ae/ventasquery/` | ✅ |
| `VentasCommand` | `https://api.dhahabi.ae/ventascommand/` | ❌ |
| `AgentesQuery` / `AgentesCommand` | `https://api.dhahabi.ae/agentes{query,command}/` | ✅ |
| `GeneralesQuery` | `https://api.dhahabi.ae/generalesquery/` | ✅ |
| `GeneralesCommand` | `https://api.dhahabi.ae/generalescommand/` | ❌ |
| `Catalogo`, `PagosCommand/Query/CubaCommand`, `ClientesQuery`, `PromocionesQuery` | `https://api.dhahabi.ae/...` | ❌ |

⚠️ `Configuration/ServiceConfiguration.cs` tiene además una API key de Google Maps hardcodeada (`AddBlazorGoogleMaps("AIzaSy...")`) — considerar moverla a configuración si se va a abrir el repo.

## Troubleshooting

### "El CSS no refleja mis cambios en Tailwind"

Revisa qué versión de `tailwindcss` corriste realmente — el `package.json` dice v3 pero el CSS actual del repo fue generado con v4 (sintaxis distinta, `@import "tailwindcss";` en vez de las directivas `@tailwind` de v3). Alinea la versión antes de asumir que el problema es tu configuración.

### "El login con Google no aparece / no funciona"

No está implementado en este proyecto — solo email/contraseña. El ícono de Google que puedas ver en `Modules/Shared/Icons/` no está conectado a ningún flujo.

## Documentación adicional

- [ARCHITECTURE.md](ARCHITECTURE.md) — arquitectura, tracking GPS, comparación con BusinessPlaceClient
- [../WORKSPACE.md](../WORKSPACE.md) — relación con BusinessPlaceServer y BusinessPlaceClient, plan de puesta en marcha del repo
- `Documentacion/ServicioLocalizacion.md` — documento detallado del sistema de tracking GPS (con diagramas)
