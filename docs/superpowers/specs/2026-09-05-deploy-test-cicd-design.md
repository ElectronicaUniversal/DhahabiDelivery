# CI/CD para BusinessPlaceServer — rama `deploy-test`

Resuelve una porción del punto 5 del backlog (`WORKFLOW.md`): CI/CD para `BusinessPlaceServer`, acotado solo al ambiente de test. La rama `deploy` para producción queda fuera de alcance — se diseña en una conversación aparte, más adelante.

## Problema

No hay forma repetible de probar cambios de backend de `BusinessPlaceServer` antes de mergearlos: los contenedores que corren hoy en el VPS (`agentes-command`, `ventas-query`, etc., en `bp-network`, contra `dhahabi-db`) son **producción real** — construidos desde `/home/dhahabi3/BP/BusinessPlaceServer` en la rama `dev-stripe`. Probar una rama en desarrollo (como `fix/delivery-app`) requiere hoy pasos manuales por SSH.

El repo ya tiene, sin usar hace ~4 meses, las piezas de un ambiente de test aislado:
- `docker-compose-t.yml` + `build-test.sh`/`build-one-test.sh`: construyen los ~19 microservicios con tag `-test` y los levantan en puertos alternos (3000-4800), conectados a un contenedor `sqlserver-docker` separado de `dhahabi-db`, compartiendo la red `bp-network` con producción.
- nginx en el VPS (`/etc/nginx/conf.d/api.dhahabi.ae.conf`) ya tiene una sección `AMBIENTE DE PRUEBA` con rutas `/<servicio>test/` (ej. `/agentescommandtest/`, `/ventasquerytest/`) apuntando a esos mismos puertos — confirmado, cubre los 14 servicios incluyendo los que toca esta feature (`agentescommandtest`→4300, `agentesquerytest`→4400, `ventascommandtest`→3600, `ventasquerytest`→3700).

Lo que falta es automatizar el build+deploy de ese stack de test al estilo de lo que ya existe para `DhahabiDelivery` (push → runner self-hosted → build), en vez de correr `build-test.sh` a mano.

## Diseño

### Runner nuevo, dedicado

Un runner de GitHub Actions self-hosted **separado** del que ya existe para `DhahabiDelivery` (no se reutiliza ese — reutilizarlo requeriría registrarlo a nivel organización, tocando infraestructura que ya funciona, con permisos más elevados de los necesarios).

- Directorio en el VPS: `~/BP/actions-runner/` (al lado del clone de producción en `~/BP/BusinessPlaceServer/`, sin tocarlo).
- Registrado específicamente contra `ElectronicaUniversal/BusinessPlaceServer` (repo privado — el riesgo de "runner self-hosted + PR de un fork" que motivó no disparar por `pull_request` en DhahabiDelivery no aplica igual acá, pero por higiene el workflow igual dispara solo por `push`, no por `pull_request`).
- Label: `dhahabi-vps-bp`.
- A diferencia del runner de DhahabiDelivery (que necesita el toolchain completo de .NET/MAUI/Android instalado en el host), este runner **solo necesita Docker** — cada microservicio hace su propio build multi-stage dentro de su Dockerfile, el host no compila nada directamente.

### Workflow

`BusinessPlaceServer/.github/workflows/deploy-test.yml` — primer archivo de CI en este repo.

- Dispara con `push` a la rama `deploy-test` únicamente.
- Corre en `runs-on: [self-hosted, dhahabi-vps-bp]`.
- Un solo job:
  1. Checkout.
  2. Paso de verificación previo (nuevo): confirma que `sqlserver-docker` está `Up` antes de seguir, y falla el job con un mensaje explícito si no lo está — sin esto, `build-test.sh` levantaría igual los microservicios y fallarían de forma confusa (crasheando/reiniciando por no poder conectar a la DB) en vez de dar un error claro y accionable ("reactivá sqlserver-docker a mano").
  3. Ejecuta `./build-test.sh` (ya existe, sin modificar su lógica de build/compose).
  4. Paso de verificación posterior (nuevo): `docker-compose -p businessplaceserver-test -f docker-compose-t.yml ps` y falla el job si algún contenedor esperado no aparece `Up` — `build-test.sh` hoy no falla el script si una imagen individual no construye (imprime ❌ pero continúa), así que sin este paso el job podría reportar éxito con servicios rotos.
- Sin secrets nuevos: no hay firma ni credenciales externas involucradas (la password de la DB de test ya está en texto plano en `docker-compose-t.yml`, preexistente, no se toca en este trabajo).

Como el runner corre físicamente en el VPS, no hace falta ningún paso de despliegue remoto (SSH, artifact download, etc.) — construir y levantar los contenedores es el mismo paso.

### Base de datos de test

`sqlserver-docker` (contenedor parado hace ~4 meses, shutdown limpio, sin volumen externo — los datos siguen en su capa de escritura) se reactiva **manualmente, una sola vez**, fuera del workflow:

```bash
docker start sqlserver-docker
```

El workflow **no gestiona la DB** — asume que ya está corriendo y conectada a `bp-network`. Recrear/migrar el schema automáticamente en cada push es un problema más grande (out of scope de este diseño); si hace falta reponer datos de prueba (repartidor, cliente, órdenes) se hace a mano, siguiendo la plantilla ya documentada en `WORKFLOW.md` (sección "Datos de prueba").

### Apuntar la app de prueba (Waydroid) al stack de test

Ya existe el ruteo nginx (`AMBIENTE DE PRUEBA`, ver arriba) — no se toca. Falta solo que la app hable con esas URLs en vez de las de producción.

Se agrega `DhahabiDelivery/Configuration/AppSettings.test.json`: mismo contenido que `AppSettings.json`, con el sufijo `test` agregado a cada path (ej. `"AgentesCommand": "https://api.dhahabi.ae/agentescommandtest/"` en vez de `.../agentescommand/`). Uso manual y puntual: copiarlo sobre `AppSettings.json` antes de un build de prueba dirigido al stack de test, y revertir después — no se integra al pipeline automático existente de DhahabiDelivery (ese sigue apuntando siempre a producción). Es una herramienta ad-hoc para testing, no un flavor de build permanente — evita la complejidad de manejar dos configuraciones de build en el mismo pipeline para un caso de uso infrecuente.

## Fuera de alcance

- Rama `deploy` y su workflow para producción — reemplazar los contenedores reales que sirven tráfico real es una decisión de mucho más riesgo (rollback, orden de despliegue de ~19 microservicios, downtime) que merece su propio diseño, no se resuelve acá.
- Gestión automática de la base de datos de test (migraciones, seed de datos) en cada push.
- Arreglar la anomalía preexistente en `docker-compose-t.yml` donde `catalogoquerytest` tiene `VIRTUAL_HOST=catalogo.dhahabi.ae` (el mismo valor que la versión de producción) — parece config muerta/heredada de una arquitectura anterior con `nginx-proxy` auto-configurado por labels (hoy el ruteo real es vía el nginx de sistema, config manual por path, no por `VIRTUAL_HOST`), pero no se confirmó que sea inofensivo con certeza total. No se toca en este trabajo; anotado para revisar si alguna vez se reintroduce ese patrón.
