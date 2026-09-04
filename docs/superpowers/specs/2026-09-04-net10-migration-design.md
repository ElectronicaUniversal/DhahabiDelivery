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
