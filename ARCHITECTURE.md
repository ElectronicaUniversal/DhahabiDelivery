# Arquitectura — DhahabiDelivery

## Qué cubre este documento

Cómo está organizada la app del repartidor, cómo se relaciona estructuralmente con BusinessPlaceClient (de donde fue forkeada), y el sistema de tracking GPS que es su feature central.

**Audiencia:** desarrolladores que van a trabajar en la app del repartidor, especialmente quienes ya conocen BusinessPlaceClient y necesitan entender en qué se parece y en qué diverge.

## Diseño del sistema

### Arquitectura de alto nivel

```
┌──────────────────────────────────────────┐
│           DhahabiDelivery (MAUI)           │
│  Un único proyecto, sin split Compartido/  │
│  Hybrid/Web (a diferencia de Client)       │
│                                             │
│  Modules/ Auth, Entregas, Scanner, Layout, │
│           Shared, Usuario                   │
│  Configuration/ AppSettings, Apis, Transition│
└───────────────────┬────────────────────────┘
                     │ HttpClient nombrado por microservicio
                     ▼
       api.dhahabi.ae/{agentes,ventas,autenticacion,generales}{query|command}
                     │
                     ▼
         BusinessPlaceServer (rol Repartidor)
                     │
                     ▼
         Google Maps (BlazorGoogleMaps) + GPS del dispositivo
```

**Componentes:**

1. **App MAUI única** — no hay separación Compartido/Hybrid/Web como en BusinessPlaceClient; todo vive en un solo proyecto `DhahabiDelivery.csproj`.
2. **LocationService + foreground service Android** — tracking GPS con frecuencia variable según el estado del repartidor.
3. **Scanner (ZXing.Net.Maui)** — lectura de código de barras/QR, presumiblemente para confirmar entregas.
4. **BusinessPlaceServer (rol Repartidor)** — el backend, consumido igual que desde BusinessPlaceClient pero con endpoints distintos (`Agentes.*`, no `Catalogo`/`Carrito`).

### Stack tecnológico

| Capa | Tecnología | Nota |
|---|---|---|
| App | .NET MAUI (Blazor Hybrid) | `net8.0-android34.0;net8.0-ios;net8.0-maccatalyst` — un release detrás del .NET 9 de Client |
| Mapas | BlazorGoogleMaps 4.9.3 | API key hardcodeada en `ServiceConfiguration.cs` |
| Scanner | ZXing.Net.Maui / .Controls 0.4.0 | Código de barras/QR |
| Resiliencia HTTP | Polly 8.4.2 | |
| Push | Pushy.SDK.MAUI.Android 1.0.93 | Misma versión que BusinessPlaceClient |
| CSS | Tailwind CSS | Único de los tres proyectos que usa Tailwind/npm |
| Mediación | MediatR 12.3.0 | Presente como dependencia; uso interno no confirmado en profundidad |

## Estructura de directorios

```
DhahabiDelivery/
└── DhahabiDelivery/                    # único proyecto
    ├── Modules/
    │   ├── Auth/           # Login, registro, recuperación — SIN Google Sign-In
    │   ├── Entregas/        # Núcleo: mapa, detalle, servicios (+ versiones Mock)
    │   ├── Scanner/         # Código de barras/QR
    │   ├── Layout/          # Layout + bottom bar
    │   ├── Shared/          # Componentes cross-cutting, LocationService, HttpHelper
    │   │   └── Pages/       # ⚠️ vacío
    │   └── Usuario/         # Perfil + estado de disponibilidad
    ├── Configuration/
    │   ├── AppSettings.json/.cs, Apis.cs, MetodosExtension.cs
    │   ├── Mensajeria.dll   # Copia independiente (no comparte con Client)
    │   └── Transition/      # Copiado de BusinessPlaceClient
    ├── Services/             # ⚠️ vacío, remanente de scaffolding
    ├── Documentacion/ServicioLocalizacion.md
    ├── Platforms/{Android,iOS,MacCatalyst,Windows,Tizen}/
    └── CameraPage.xaml(.cs)  # suelto en la raíz, no modularizado
```

### Reglas de cada carpeta

#### Modules/Entregas/

**Qué va acá:** todo lo relacionado con recibir, visualizar y completar una entrega — mapa, secciones de detalle, servicios (`EntregasService`, `RepartidorService`) y sus mocks.

**Patrón Mock**: cada servicio principal tiene una contraparte `*ServiceMock` — usarlas para desarrollar/probar UI sin depender del backend real.

#### Configuration/

**Qué va acá:** arranque, `AppSettings`, rutas (`Apis.cs`), y el router de transiciones. Es una copia casi literal de la carpeta homónima en `BusinessPlaceClient/FrontentCompartido/Configuration/` — si BusinessPlaceClient cambia una convención acá, este proyecto no se entera automáticamente (no hay dependencia real entre los dos repos).

## Flujo de datos

### Tracking de posición del repartidor

```
LocationService (singleton, DI)
      │
      ├─ NO_DISPONIBLE → no envía posición
      ├─ DISPONIBLE     → envía cada 30s
      └─ ENTREGANDO     → envía cada 5s
      │
      ▼
LocationForegroundServiceFixed (Android, notificación persistente)
      │  usa GPS + Network providers
      ▼
POST AgentesCommand /Repartidor/ActualizarPosicionRepartidor
      │
      ▼
BusinessPlaceServer actualiza Repartidor.UltimaPosicion
      │
      ▼
BusinessPlaceClient / backoffice puede consultar
POST AgentesQuery /Repartidor/ObtenerUltimaPosicionRepartidor
```

Permisos Android requeridos (`AndroidManifest.xml`): `ACCESS_FINE_LOCATION`, `ACCESS_COARSE_LOCATION`, `ACCESS_BACKGROUND_LOCATION`, `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_LOCATION` (Android 14+), `CAMERA` (para el scanner).

Manejo de errores: si el GPS está deshabilitado, `GpsUtils` lanza `GpsNotEnabledException`, capturada en la UI para mostrar un diálogo con acción "abrir configuración".

### Ciclo de una entrega (lado repartidor)

```
1. Backoffice asigna Repartidor a una Orden (ver BusinessPlaceServer/AsignarDeliveryHandler)
2. DhahabiDelivery consulta AgentesQuery/VentasQuery para ver entregas asignadas
3. Repartidor marca EstablecerEstadoRepartidor → ENTREGANDO (empieza tracking cada 5s)
4. Al llegar, usa Modules/Scanner para escanear código de la entrega
5. Marca la entrega como completada (EntregaCompletadaSection) → vuelve a DISPONIBLE
```

## Comparación estructural con BusinessPlaceClient

Este proyecto fue scaffoldeado copiando el patrón de `FrontentHybrid` + `FrontentCompartido`, no tomándolos como dependencia. Evidencia concreta:

| Aspecto | BusinessPlaceClient | DhahabiDelivery |
|---|---|---|
| Estructura de proyectos | 3 (`FrontentCompartido`/`Hybrid`/`Web`) | 1 (`DhahabiDelivery`) |
| Target framework | `net9.0-*` | `net8.0-*` (un release atrás) |
| `Mensajeria.dll` | `FrontentCompartido/Lib/Mensajeria.dll`, referenciado por los 3 proyectos | `Configuration/Mensajeria.dll`, copia independiente |
| `Configuration/Transition/` | Original | Copia idéntica (mismos 4 archivos) |
| `ApplicationId` | `com.dhahabimarket.dhahabi` | `com.dhahabimarket.dhahabi.delivery` (mismo prefijo) |
| `Dhahabi.ViewModel` | v1.0.5 | v1.0.5 (idéntica) |
| Google Auth | Completo (4 docs, MAUI + Web) | No implementado (icono sin conectar) |
| Firma Android | Activa (`SimpleMAUIApp.keystore`) | Comentada/inactiva (`dhahabi-delivery.keystore`) |
| Namespace leftover | — | `ImageService-interface.cs` con `namespace FrontentCompartido...` sin corregir |

**Implicación práctica:** no asumas que un fix en `BusinessPlaceClient/FrontentCompartido/Configuration/` se refleja acá — hay que portarlo a mano, igual que con `Mensajeria.dll`.

## Decisiones de diseño clave

### Decisión 1: Un solo proyecto MAUI en vez de replicar el split Compartido/Hybrid/Web

**Qué se decidió:** DhahabiDelivery es un único `.csproj`, sin librería compartida separada.

**Contexto:** a diferencia de la tienda, no hay (todavía) una versión web de la app del repartidor — solo mobile.

**Trade-offs:**
- ✅ Menos complejidad de proyecto para un caso de uso mobile-only.
- ❌ Si en el futuro se necesita una versión web (ej. panel de repartidores en escritorio), habría que hacer el split después, migrando código ya escrito — no hay una `FrontentCompartido`-equivalente separada desde el día uno.

### Decisión 2 (implícita, a revisar): DLL de contratos copiado en vez de referenciar BusinessPlaceClient

**Qué se decidió (de facto):** en vez de depender de `BusinessPlaceClient/FrontentCompartido/Lib/Mensajeria.dll` o de la fuente `Mensajeria` del Server, este proyecto tiene su propia copia en `Configuration/Mensajeria.dll`.

**Por qué importa:** son **tres** copias del mismo DLL en el ecosistema (Server lo compila, Client lo copia, Delivery lo copia por separado) — el mismo problema de sincronización manual descrito en [../WORKSPACE.md](../WORKSPACE.md), pero duplicado.

## Seguridad

- **Sin Google Auth** — superficie de ataque de auth más chica que Client, pero también menos conveniente para el usuario.
- **API key de Google Maps hardcodeada** en `ServiceConfiguration.cs` — considerar restringirla por paquete/huella en la consola de Google Cloud si no está ya restringida, dado que está en código fuente.
- **Password de keystore en comentario del `.csproj`** (`hallenkami412120`) — aunque el bloque de firma está comentado/inactivo, la contraseña ya quedó en el historial de archivos locales; rotarla si se decide activar la firma antes de crear el repo.

## Despliegue

No hay pipeline de CI/CD (no hay repo git). Build y firma son enteramente manuales hoy — ver [Estado del proyecto en README.md](README.md#estado-del-proyecto) para lo que falta antes de poder automatizar esto.

## Troubleshooting arquitectónico

**Problema: "el repartidor no ve una entrega recién asignada"**
- Revisar que `AgentesQuery`/`VentasQuery` tengan `HttpClient` registrado en `ServiceConfiguration.cs` (sí lo tienen) y que el estado del `Repartidor` en el Server sea el esperado (`ASIGNADO`) — comparar constantes con `BusinessPlaceServer/DataAccessLayer/Constantes/ConstantesEstadoRepartidor.cs`.

**Problema: "quiero agregar pagos/catálogo a esta app y el `HttpClient` no está"**
- Ver README § Configuración — `PagosCommand/Query`, `Catalogo`, `PromocionesQuery`, `ClientesQuery` y `VentasCommand` están en `AppSettings.json` pero no registrados como `HttpClient` en `ServiceConfiguration.cs`. Agregar la línea correspondiente antes de intentar inyectar esos clientes.

## Recursos adicionales

- [README.md](README.md) — arranque rápido, estado del proyecto
- [../WORKSPACE.md](../WORKSPACE.md) — relación con BusinessPlaceServer y BusinessPlaceClient, plan de puesta en marcha del repo
- `Documentacion/ServicioLocalizacion.md` — documento original y detallado del sistema de tracking GPS
