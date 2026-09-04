# DhahabiDelivery: Migración a .NET 10 (Android)

## Objetivo

Migrar `DhahabiDelivery` (MAUI Android) de `net8.0-android34.0` a .NET 10,
subiendo también el nivel de API de Android objetivo. El build/CI (GitHub
Actions) ya está funcionando en .NET 8 con firma vía secrets y trimming
habilitado (~46MB por artifact, 4 arquitecturas: arm, arm64, x86, x64).

## Restricciones

- No se puede compilar localmente (disco local al 93%, sin espacio para el
  workload MAUI de .NET 10).
- No se quiere depender solo de GitHub Actions para iterar (consumo de
  minutos del plan mensual).
- Se dispone de un VPS (Contabo, `dhahabi.ae`, usuario `dhahabi3`, puerto 33)
  con 637GB libres / 47GB RAM / 12 cores — es un servidor de **producción
  compartido** (Odoo, MinIO, mssql-server, correo, n8n, nginx, etc.), por lo
  que toda la instalación debe quedar aislada y ser trivialmente reversible.

## Enfoque

Todo el toolchain de migración vive en una única carpeta autocontenida en el
VPS: `~/dhahabi-net10-migration/`. Nada toca `apt` ni rutas globales del
sistema (`~/.dotnet`, `~/.nuget`, etc.). Limpieza = `rm -rf` de esa carpeta.

```
dhahabi-net10-migration/
├── dotnet10/        SDK .NET 10 (dotnet-install.sh --install-dir)
├── jdk17/           Java 17 portable (tarball, sin apt)
├── android-sdk/     Android cmdline-tools + platform/build-tools
├── nuget-packages/  cache de NuGet redirigido aquí
├── DhahabiDelivery/ clone del repo, rama fix/delivery-app
└── env.sh           exporta DOTNET_ROOT, JAVA_HOME, ANDROID_HOME,
                      NUGET_PACKAGES, PATH
```

## Pasos

1. Instalar .NET 10 SDK aislado.
2. Instalar Java 17 portable (sin apt).
3. Instalar Android SDK cmdline-tools + platform/build-tools aislado.
4. Agregar workload `maui-android` al SDK .NET 10.
5. Clonar `DhahabiDelivery` (rama `fix/delivery-app`) dentro de la carpeta.
6. Actualizar `global.json` a .NET 10 y el `TargetFramework` del `.csproj`
   de `net8.0-android34.0` al nivel de API más reciente soportado por el
   workload de .NET 10.
7. Iterar `dotnet restore`/`build` contra `net10.0-android` en el VPS,
   corrigiendo incompatibilidades de paquetes (`ZXing.Net.Maui`,
   `Pushy.SDK.MAUI.Android`, `BlazorGoogleMaps`, `Dhahabi.ViewModel`, etc.)
   hasta que compile en verde.
8. Build final firmado (AAB/APK, 4 arquitecturas) — en el VPS o confirmado
   una sola vez vía el workflow de GitHub Actions ya existente
   (`.github/workflows/android-release.yml`), actualizando ahí también
   `dotnet-version` a `10.0.x`.

## Fuera de alcance

- No se toca `BusinessPlaceClient`/`FrontentHybrid` (siguen en .NET 9).
- No se instala nada system-wide en el VPS.

## Resultado (2026-09-04)

`net10.0-android36.0` compila y publica sin problema para una sola
arquitectura (paquetes `ZXing.Net.Maui`, `Pushy.SDK.MAUI.Android`,
`BlazorGoogleMaps`, `Dhahabi.ViewModel` etc. son compatibles sin cambios de
código). Pero el publish multi-RID (4 arquitecturas en un solo build, lo que
usa el workflow para generar el AAB/APK de release) falla con **NU1102**:
NuGet intenta restaurar `Microsoft.NETCore.App.Runtime.Mono.linux-x64`
versión `10.0.11`, paquete que Microsoft nunca publicó en nuget.org. Es un
bug confirmado y sin resolver en el SDK/workload de .NET 10
([dotnet/maui#27215](https://github.com/dotnet/maui/issues/27215),
reproducido en .NET 10 según el comentario más reciente del hilo). El
workaround documentado (`UseMonoRuntime=false`, runtime CoreCLR experimental
para Android) solo soporta arquitecturas de 64 bits y además falla con un
segundo error (`XA0035`) en el publish multi-RID con el SDK 10.0.400 actual.

**Decisión:** migrar a **.NET 9** (`net9.0-android35.0`) en vez de .NET 10.
Verificado en el VPS: publish multi-RID (4 arquitecturas) funciona limpio,
produce un AAB firmado de ~42MB con las 4 ABIs (`arm64-v8a`, `armeabi-v7a`,
`x86`, `x86_64`). `global.json`, `DhahabiDelivery.csproj` y
`.github/workflows/android-release.yml` actualizados a net9. La migración a
.NET 10 queda pendiente hasta que Microsoft resuelva el bug de arriba — el
toolchain de .NET 10 instalado en el VPS (`dotnet10/`, packs, workload) se
deja documentado para retomar rápido cuando corresponda.
