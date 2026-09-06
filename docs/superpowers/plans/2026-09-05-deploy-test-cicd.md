# CI/CD de test para BusinessPlaceServer (`deploy-test`) — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que un `git push` a la rama `deploy-test` de `BusinessPlaceServer` reconstruya y levante automáticamente el stack de test aislado (`docker-compose-t.yml`) en el VPS, vía un runner de GitHub Actions self-hosted dedicado.

**Architecture:** Runner nuevo y separado (Docker-only, sin toolchain de .NET/Android) registrado contra `ElectronicaUniversal/BusinessPlaceServer`, corriendo `./build-test.sh` (ya existe) en cada push a `deploy-test`, con un pre-check (la DB de test debe estar arriba) y un post-check (todos los contenedores del stack de test deben quedar `Up`) alrededor de ese script. El ruteo nginx que expone estos servicios en `api.dhahabi.ae/<servicio>test/` ya existe y no se toca.

**Tech Stack:** GitHub Actions (self-hosted runner), Docker + Docker Compose v2, bash.

**Spec:** `docs/superpowers/specs/2026-09-05-deploy-test-cicd-design.md`

**Acceso al VPS:** `ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae` — usuario `dhahabi3` tiene sudo sin password ahí (confirmado). Es un servidor de producción compartido (Odoo, otros microservicios, etc.) — todo lo de este plan vive autocontenido en el home de `dhahabi3`, no se toca nada a nivel de sistema salvo el servicio systemd del runner nuevo.

**Nota sobre `~/BP/`:** el spec sugería `~/BP/actions-runner/` como ubicación, pero `~/BP/` y `~/BP/BusinessPlaceServer/` resultaron ser propiedad de `root:root` (no escribibles por `dhahabi3` sin sudo) — probablemente de cuando se restauró el dump de la base de datos ahí. Este plan usa `~/actions-runner-bp/` (directo bajo el home de `dhahabi3`, mismo nivel que `~/dhahabi-net10-migration/`) para evitar depender de permisos de `root` en una carpeta que no gestiona este trabajo.

---

## Task 1: Generar el token de registro del runner (paso manual, requiere admin del repo)

**Por qué es manual:** registrar un runner self-hosted requiere un token de registro que solo se puede generar con permisos de **admin** sobre el repo. La cuenta de GitHub autenticada en esta sesión (`gh auth status`) tiene `push`/`pull`/`triage` sobre `ElectronicaUniversal/BusinessPlaceServer` pero no `admin` — no puede generarlo vía `gh api`. Este paso lo tiene que hacer quien sí tenga admin sobre el repo.

- [ ] **Step 1: Generar el token desde la UI de GitHub**

Ir a `https://github.com/ElectronicaUniversal/BusinessPlaceServer/settings/actions/runners/new`, elegir **Linux** / **x64**. GitHub muestra un comando `./config.sh --url ... --token AAAAAAAAAAAAAAAAAAAAAAAAAAAAA` — copiar solo el valor del token (empieza con `A`, ~29-30 caracteres). El token expira en ~1 hora, así que generarlo justo antes del Task 2.

- [ ] **Step 2: Pasar el token a quien ejecuta el Task 2**

El token generado en el Step 1 se usa directamente en el `config.sh` del Task 2, Step 4 — no se guarda en ningún archivo del repo ni se commitea en ningún lado.

---

## Task 2: Registrar el runner self-hosted dedicado en el VPS

**Requiere:** el token del Task 1 (fresco, generado en la última hora).

- [ ] **Step 1: Crear el directorio del runner**

```bash
ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae 'mkdir -p ~/actions-runner-bp'
```

- [ ] **Step 2: Resolver la última versión del runner y descargarlo**

```bash
ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae '
cd ~/actions-runner-bp
LATEST=$(curl -s https://api.github.com/repos/actions/runner/releases/latest | grep -oP "\"tag_name\": \"v\K[0-9.]+")
echo "Versión resuelta: $LATEST"
curl -o actions-runner-linux-x64.tar.gz -L "https://github.com/actions/runner/releases/download/v${LATEST}/actions-runner-linux-x64-${LATEST}.tar.gz"
tar xzf actions-runner-linux-x64.tar.gz
'
```

Expected: termina sin error, `ls ~/actions-runner-bp` muestra `config.sh`, `run.sh`, `bin/`, etc.

- [ ] **Step 3: Verificar arquitectura del VPS coincide con el paquete descargado**

Ya confirmado en la investigación previa: `uname -m` → `x86_64`, coincide con `linux-x64`. No hace falta reverificar, solo dejarlo anotado por si se repite este proceso en otra máquina.

- [ ] **Step 4: Configurar el runner (usar el token del Task 1)**

```bash
ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae '
cd ~/actions-runner-bp
./config.sh --url https://github.com/ElectronicaUniversal/BusinessPlaceServer --token <TOKEN_DEL_TASK_1> --name dhahabi-vps-bp --labels dhahabi-vps-bp --work _work --unattended
'
```

Expected: termina con `√ Connected to GitHub` y `√ Runner successfully added` / `√ Runner connection is good`.

Si el token expiró (más de ~1 hora desde que se generó), este comando falla con `Http response code: NotFound` o similar — volver al Task 1 y generar uno nuevo.

- [ ] **Step 5: Instalar y arrancar como servicio systemd**

```bash
ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae '
cd ~/actions-runner-bp
sudo ./svc.sh install dhahabi3
sudo ./svc.sh start
sudo ./svc.sh status
'
```

Expected: el status muestra `active (running)`.

- [ ] **Step 6: Confirmar que el runner aparece "Idle" en GitHub**

```bash
gh api repos/ElectronicaUniversal/BusinessPlaceServer/actions/runners --jq '.runners[] | {name, status, labels: [.labels[].name]}'
```

Expected: un runner con `"name": "dhahabi-vps-bp"`, `"status": "online"`, labels incluyendo `dhahabi-vps-bp`.

---

## Task 3: Crear el workflow `deploy-test.yml`

**Files:**
- Create: `BusinessPlaceServer/.github/workflows/deploy-test.yml`

- [ ] **Step 1: Escribir el workflow**

Crear `.github/workflows/deploy-test.yml`:

```yaml
name: Deploy Test Stack

on:
  push:
    branches:
      - deploy-test

jobs:
  deploy-test:
    runs-on: [self-hosted, dhahabi-vps-bp]
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Verificar que la base de datos de test esté corriendo
        run: |
          if [ "$(docker inspect -f '{{.State.Running}}' sqlserver-docker 2>/dev/null)" != "true" ]; then
            echo "::error::El contenedor sqlserver-docker no está corriendo. Reactivarlo manualmente con 'docker start sqlserver-docker' antes de reintentar este workflow."
            exit 1
          fi
          echo "sqlserver-docker está Up, continuando."

      - name: Construir y levantar el stack de test
        run: |
          chmod +x ./build-test.sh
          ./build-test.sh

      - name: Verificar que todos los contenedores del stack de test quedaron Up
        run: |
          EXPECTED_CONTAINERS="catalogo-command-test catalogo-query-test clientes-command-test clientes-query-test generales-command-test generales-query-test ventas-command-test ventas-query-test autenticacion-command-test autenticacion-query-test pagos-command-test pagoscuba-command-test pagos-query-test agentes-command-test agentes-query-test reportes-query-test promociones-query-test"
          FAILED=0
          for c in $EXPECTED_CONTAINERS; do
            STATE=$(docker inspect -f '{{.State.Running}}' "$c" 2>/dev/null || echo "missing")
            if [ "$STATE" != "true" ]; then
              echo "::error::Contenedor $c no está Up (estado: $STATE)"
              FAILED=1
            fi
          done
          if [ "$FAILED" -eq 1 ]; then
            echo "::error::Uno o más contenedores del stack de test no quedaron Up. Ver 'docker logs <nombre>' en el VPS para la causa."
            exit 1
          fi
          echo "Los 17 contenedores del stack de test están Up."
```

- [ ] **Step 2: Verificar la sintaxis YAML localmente**

```bash
cd /home/hallen/Dhahabi/BusinessPlaceServer
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/deploy-test.yml'))" && echo "YAML válido"
```

Expected: `YAML válido` (sin excepción de parseo).

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/deploy-test.yml
git commit -m "Agregar workflow de CI para el stack de test (rama deploy-test)"
```

---

## Task 4: Crear la rama `deploy-test` y probar el workflow end-to-end

**Requiere:** Task 2 (runner activo) y Task 3 (workflow commiteado) completos. Requiere también que `sqlserver-docker` esté corriendo — si el Task 5 (reactivarla) todavía no se hizo, este task va a fallar en el pre-check del workflow a propósito (eso confirma que el pre-check funciona) — hacer el Task 5 antes de este si se quiere ver el flujo completo en verde la primera vez.

- [ ] **Step 1: Crear y pushear la rama**

```bash
cd /home/hallen/Dhahabi/BusinessPlaceServer
git checkout -b deploy-test
git push -u origin deploy-test
```

Expected: push exitoso, GitHub dispara el workflow automáticamente (visible en Actions del repo).

- [ ] **Step 2: Seguir la corrida**

```bash
gh run list --repo ElectronicaUniversal/BusinessPlaceServer --branch deploy-test --limit 1
gh run watch $(gh run list --repo ElectronicaUniversal/BusinessPlaceServer --branch deploy-test --limit 1 --json databaseId --jq '.[0].databaseId') --repo ElectronicaUniversal/BusinessPlaceServer
```

Expected: los tres steps (pre-check DB, build-test.sh, post-check contenedores) en verde. Si el pre-check falla porque `sqlserver-docker` no está arriba, es el comportamiento esperado si el Task 5 no se hizo todavía — no es un bug del workflow.

- [ ] **Step 3: Volver a `fix/delivery-app` en el checkout local**

```bash
git checkout fix/delivery-app
```

(No hace falta mergear `deploy-test` a ninguna rama — es una rama de disparo de CI, no de código; su único propósito es que un push ahí dispare el deploy del stack de test. Para desplegar cambios de otra rama, se hace `git push origin <esa-rama>:deploy-test` cuando haga falta probar algo — eso no se automatiza en este plan.)

---

## Task 5: Reactivar `sqlserver-docker` (manual, una sola vez)

- [ ] **Step 1: Arrancar el contenedor**

```bash
ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae 'docker start sqlserver-docker'
```

- [ ] **Step 2: Confirmar que arrancó bien**

```bash
ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae 'docker logs sqlserver-docker --tail 20'
```

Expected: última línea del estilo `SQL Server is now ready for client connections`, sin errores de arranque.

- [ ] **Step 3: Verificar que responde y ver qué schema/datos tiene**

```bash
ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae "docker exec sqlserver-docker /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Liso12345*' -C -Q \"SELECT name FROM sys.databases\" -W"
```

Expected: la lista incluye `BPDatabase`. Si no aparece, hay que restaurar el schema/datos ahí — fuera de alcance de este plan (ver spec, sección "Fuera de alcance"); reportar el hallazgo antes de seguir.

- [ ] **Step 4: Si `BPDatabase` existe, verificar que tiene las tablas relevantes**

```bash
ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae "docker exec sqlserver-docker /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Liso12345*' -C -Q \"USE BPDatabase; SELECT COUNT(*) AS Repartidores FROM BP_AGENTES.REPARTIDOR; SELECT COUNT(*) AS Ordenes FROM BP_VENTAS.ORDEN;\" -W"
```

Expected: números mayores a 0 en ambas consultas. Si están vacías o la tabla no existe, hay que reponer datos de prueba a mano (ver `WORKFLOW.md`, sección "Datos de prueba", plantilla SQL para crear un repartidor/orden de prueba) — no es parte de este plan, es la preparación de datos previa a probar la feature de multi-orden.

No hay commit en este task — es un cambio de estado en el VPS, no de código.

---

## Task 6: `AppSettings.test.json` para apuntar la app al stack de test

**Files:**
- Create: `DhahabiDelivery/Configuration/AppSettings.test.json`

- [ ] **Step 1: Crear el archivo**

Mismo contenido que `DhahabiDelivery/Configuration/AppSettings.json`, agregando el sufijo `test` a cada segmento de path (no al dominio, no al `ImageServer` que no tiene equivalente de test). Ojo con `Catalogo`: en el original apunta a `.../catalogoquery/` (es la ruta *query*, a pesar de que la clave no lo dice) — el equivalente de test es `catalogoquerytest`, no `catalogocommandtest`:

```json
{
  "ImageServer": "https://cdn.dhahabi.ae/images/",
  "VentasCommand": "https://api.dhahabi.ae/ventascommandtest/",
  "VentasQuery": "https://api.dhahabi.ae/ventasquerytest/",
  "AutenticationQuery": "https://api.dhahabi.ae/autenticacionquerytest/",
  "AutenticationCommand": "https://api.dhahabi.ae/autenticacioncommandtest/",
  "Catalogo": "https://api.dhahabi.ae/catalogoquerytest/",
  "PagosCommand": "https://api.dhahabi.ae/pagoscommandtest/",
  "PagosCubaCommand": "https://api.dhahabi.ae/pagoscubacommandtest/",
  "PagosQuery": "https://api.dhahabi.ae/pagosquerytest/",
  "ClientesQuery": "https://api.dhahabi.ae/clientesquerytest/",
  "GeneralesQuery": "https://api.dhahabi.ae/generalesquerytest/",
  "GeneralesCommand": "https://api.dhahabi.ae/generalescommandtest/",
  "PromocionesQuery": "https://api.dhahabi.ae/promocionesquerytest/",
  "AgentesQuery": "https://api.dhahabi.ae/agentesquerytest/",
  "AgentesCommand": "https://api.dhahabi.ae/agentescommandtest/"
}
```

- [ ] **Step 2: Verificar que cada URL tiene una ruta nginx real correspondiente en el VPS**

```bash
ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae 'grep -oP "location /\K[a-z]+test(?=/)" /etc/nginx/conf.d/api.dhahabi.ae.conf | sort -u'
```

Expected: la lista incluye `ventascommandtest`, `ventasquerytest`, `autenticacionquerytest`, `catalogoquerytest`, `pagoscommandtest`, `pagoscubacommandtest`, `pagosquerytest`, `clientesquerytest`, `generalesquerytest`, `generalescommandtest`, `promocionesquerytest`, `agentesquerytest`, `agentescommandtest`. Si falta `autenticacioncommandtest` o `clientescommandtest` en esa lista pero el archivo los referencia, anotarlo — puede que esos dos servicios no tengan variante de comando en `AppSettings.json` original tampoco (confirmar contra el original antes de asumir que es un error).

- [ ] **Step 3: Commit**

```bash
cd /home/hallen/Dhahabi/DhahabiDelivery
git add DhahabiDelivery/Configuration/AppSettings.test.json
git commit -m "Agregar AppSettings.test.json para apuntar builds ad-hoc al stack de test"
```

---

## Task 7: Documentar el flujo de uso en WORKFLOW.md

**Files:**
- Modify: `WORKFLOW.md`

- [ ] **Step 1: Agregar una sección nueva describiendo el flujo de test**

Agregar, después de la sección "Cómo hacer y probar un cambio (runbook)":

```markdown
## Cómo probar un cambio de backend (BusinessPlaceServer) antes de mergear

1. `sqlserver-docker` debe estar corriendo en el VPS (`docker start sqlserver-docker` si no — ver Task 5 de `DhahabiDelivery/docs/superpowers/plans/2026-09-05-deploy-test-cicd.md` para el detalle).
2. Pushear la rama a probar a `deploy-test`: `git push origin <tu-rama>:deploy-test` (fuerza el push si `deploy-test` ya tiene otro contenido: `git push -f origin <tu-rama>:deploy-test`).
3. Esto dispara el runner `dhahabi-vps-bp` (self-hosted, registrado en `ElectronicaUniversal/BusinessPlaceServer`), que reconstruye y levanta el stack de test (`docker-compose-t.yml`) en el VPS.
4. Los endpoints quedan disponibles en `https://api.dhahabi.ae/<servicio>test/` (ej. `https://api.dhahabi.ae/agentescommandtest/`).
5. Para que la app de DhahabiDelivery hable con ese stack: copiar `DhahabiDelivery/Configuration/AppSettings.test.json` sobre `DhahabiDelivery/Configuration/AppSettings.json`, hacer un build de prueba, instalar en Waydroid, probar, y **revertir el AppSettings.json** antes de commitear cualquier otra cosa (no mezclar el archivo de test con cambios reales).
6. Runner: `ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae 'sudo systemctl status actions.runner.ElectronicaUniversal-BusinessPlaceServer.dhahabi-vps-bp'` para revisar su estado si algo no dispara.
```

- [ ] **Step 2: Commit**

```bash
git add WORKFLOW.md
git commit -m "Documentar el flujo de prueba de backend vía deploy-test"
```
