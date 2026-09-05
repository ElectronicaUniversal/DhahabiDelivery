# DhahabiDelivery: Toggle manual de tema claro/oscuro

## Objetivo

Agregar un switch en la página de Perfil (`Modules/Usuario/Pages/PaginaUsuario.razor`)
que permita al usuario elegir entre tema claro y oscuro, en vez de depender
únicamente de la preferencia del sistema operativo como ocurre hoy.

## Estado actual (investigado)

- El theming de la app se basa en variables CSS (`--background`, `--text`,
  `--card-background`, etc., definidas en `wwwroot/css/app.css`) mapeadas a
  utilidades de Tailwind vía `@theme` (`bg-background`, `text-text`, ...).
- El bloque `@media (prefers-color-scheme: dark) { :root { ... } }` redefine
  esas variables cuando el sistema operativo está en modo oscuro. Es el único
  mecanismo de theming oscuro que existe hoy; no hay ninguna clase, JS,
  `localStorage` ni entrada en `Preferences` relacionada con tema.
- El proyecto compila con **Tailwind v4.0.12** en la práctica (`@import
  "tailwindcss";` + `@theme` es sintaxis v4), aunque `package.json` y
  `tailwind.config.js` quedaron desactualizados apuntando a v3.3.3 (ya
  documentado como gotcha conocido en `README.md`). Esto importa porque en
  v4 no existe la opción `darkMode: 'class'` de `tailwind.config.js` de v3;
  el equivalente v4 es la directiva CSS `@custom-variant`.
- El CSS compilado (`wwwroot/css/app.min.css`) no se genera en CI — se
  compila localmente con `npx tailwindcss -i ./wwwroot/css/app.css -o
  ./wwwroot/css/app.min.css --minify` y se commitea el resultado.
- Patrón existente de JS interop en el proyecto: módulos ES en `wwwroot/js/`
  importados vía `IJSRuntime.InvokeAsync<IJSObjectReference>("import", ...)`
  (ver `LeafletMap.razor`, `Dialog.razor`).
- Servicios de la app se registran `Scoped` en
  `Configuration/ServiceConfiguration.cs` (ej. `IStorageService`).
- `Modules/Routes.razor` es el componente raíz real: envuelve tanto el login
  como las páginas autenticadas (`MainLayout` es el layout default solo de
  las rutas autorizadas, pero `Routes.razor` se renderiza siempre).

## Decisiones de producto

- **Dos estados, no tres**: Claro / Oscuro. No hay opción "Seguir sistema".
- **Default sin preferencia guardada: tema claro**, sin consultar
  `prefers-color-scheme`. El usuario debe elegir explícitamente oscuro.
- **Ubicación**: una fila nueva en el menú de Perfil, con el mismo estilo que
  las filas existentes (Ayuda, Estado, Cambiar contraseña), ícono a la
  izquierda y un switch a la derecha.
- El cambio de tema debe aplicar a **toda la app**, no solo a la página de
  Perfil, y debe persistir entre reinicios de la app.

## Diseño técnico

### 1. `wwwroot/css/app.css`

- Agregar, junto a `@import "tailwindcss";`:
  ```css
  @custom-variant dark (&:where(.dark, .dark *));
  ```
  Esto le dice a Tailwind v4 que el variant `dark:` se activa por la
  presencia de la clase `.dark` en un ancestro, no por
  `prefers-color-scheme`.

- Cambiar el bloque de variables oscuras de:
  ```css
  @media (prefers-color-scheme: dark) {
      :root { --background: #181818; ... }
  }
  ```
  a:
  ```css
  :root.dark {
      --background: #181818; ...
  }
  ```
  Sin la clase `dark` en `<html>`, `:root` conserva los valores claros
  definidos arriba — esto ya nos da el default "claro sin consultar el SO"
  gratis.

### 2. Recompilación de CSS

Ejecutar `npx tailwindcss -i ./wwwroot/css/app.css -o
./wwwroot/css/app.min.css --minify` (usando la v4 instalada en
`node_modules`, no la v3 declarada en `package.json`) y commitear el
`app.min.css` resultante como parte del mismo cambio.

### 3. `wwwroot/js/theme.js` (nuevo)

Módulo ES con:
- `getIsDark()`: lee `localStorage.getItem('dhahabi-theme')`; devuelve
  `true` solo si el valor es `'dark'`, `false` en cualquier otro caso
  (incluido `null`/sin valor guardado → default claro).
- `setIsDark(isDark)`: escribe `'dark'`/`'light'` en `localStorage` y aplica
  la clase (`document.documentElement.classList.toggle('dark', isDark)`).
- `applyStoredTheme()`: llama a `getIsDark()` y aplica la clase
  correspondiente. Pensada para invocarse una vez al arrancar la app.

Se usa `localStorage` (no `Preferences`/`IStorageService`) porque es lo que
permite leer y aplicar el tema inmediatamente desde JS sin una ida y vuelta
de interop a C# antes de poder pintar la clase en el `<html>`.

### 4. `IThemeService` / `ThemeService` (nuevo, `Scoped`)

En `Modules/Shared/Services/`, siguiendo el mismo patrón de import de módulo
que `LeafletMap.razor`/`Dialog.razor`:
- `Task<bool> GetIsDarkAsync()`
- `Task SetIsDarkAsync(bool isDark)`
- `Task ApplyStoredThemeAsync()`

Registrar en `Configuration/ServiceConfiguration.cs`:
`services.AddScoped<IThemeService, ThemeService>();`

### 5. Aplicación al arrancar (`Modules/Routes.razor`)

Inyectar `IThemeService` y, en `OnAfterRenderAsync(firstRender)`, llamar a
`ApplyStoredThemeAsync()`. Al estar en el componente raíz, cubre tanto login
como las páginas autenticadas. Como la app muestra una pantalla de carga
(lottie) antes de que Blazor pinte contenido real, no hay flash de tema
incorrecto perceptible en este alcance.

### 6. UI en `PaginaUsuario.razor`

Nueva fila dentro del mismo `<div class="w-full flex flex-col gap-2 p-2
bg-card-background shadow rounded-2xl">` que las filas de Ayuda/Estado/Cambiar
contraseña:
- Ícono sol/luna a la izquierda (SVG inline, mismo estilo `bi-*` que las
  otras filas) que refleja el estado actual.
- Un switch a la derecha: `<input type="checkbox">` oculto + `<label>`
  estilizado con Tailwind (`peer`/`peer-checked`), usando los tokens de color
  existentes (`bg-primary` para el estado activo, `bg-divider` para el
  inactivo) — no se crea un componente reutilizable nuevo porque es el único
  uso en la app.
- `@code`: campo `_isDark` inicializado en `OnInitializedAsync` vía
  `ThemeService.GetIsDarkAsync()`; al togglear el switch se llama a
  `ThemeService.SetIsDarkAsync(nuevoValor)` y se actualiza `_isDark` para
  re-renderizar.

## Fuera de alcance

- Opción "seguir sistema" (explícitamente descartada).
- Persistencia vía `Preferences`/`IStorageService` (se usa `localStorage`,
  ver justificación arriba).
- Nuevo componente `Switch` reutilizable (un solo uso hoy).
- Revisar/limpiar los usos sueltos de `dark:` que ya existen en
  `index.html`, `SelectorEstadoDelivery.razor.css`, etc. — seguirán
  funcionando igual, solo que ahora activados por la clase `.dark` en vez de
  `prefers-color-scheme` (que es exactamente el comportamiento buscado).

## Testing

- Manual: togglear el switch en Perfil, confirmar que cambia el tema en esa
  página y en otras (Home, Entregas) sin recargar. Cerrar y reabrir la app
  (o recargar el WebView) y confirmar que el tema persiste. Confirmar que
  una instalación limpia (sin `localStorage` previo) arranca en claro.
