# CI/CD de producción para BusinessPlaceServer (`deploy`) — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **⚠️ Task 2 de este plan empuja código a producción real (contenedores en vivo sirviendo tráfico real).** Esa tarea NO se ejecuta automáticamente bajo ninguna circunstancia — se detiene y se le pide confirmación explícita al humano antes del `git push`, sin excepción, incluso en modo subagent-driven-development donde normalmente no se pausa entre tareas.

**Goal:** Que un `git push` a la rama `deploy` de `BusinessPlaceServer` reconstruya y despliegue automáticamente el stack de producción real (`docker-compose.yml`, base de datos `dhahabi-db`, contenedores con `restart: always`), vía el runner de GitHub Actions self-hosted `dhahabi-vps-bp` que ya existe y corre `deploy-test`.

**Architecture:** Un solo workflow nuevo (`deploy.yml`), hermano de `deploy-test.yml`, en el mismo runner. Pre-check de que `dhahabi-db` está `Up`, deploy vía `docker-compose -f docker-compose.yml up -d --build` (sin `down` previo, para minimizar downtime reemplazando servicio por servicio en vez de apagar todo el stack), post-check de que los ~19 contenedores de producción reales quedaron `Up`. Sin rollback automático, sin migraciones automatizadas, sin aprobación manual en GitHub — mismo nivel de manejo de errores que `deploy-test`, ya validado.

**Tech Stack:** GitHub Actions (self-hosted runner existente), Docker + Docker Compose v2 (binario `docker-compose`, igual que `build-test.sh`), bash.

**Spec:** `docs/superpowers/specs/2026-09-06-deploy-prod-cicd-design.md`

**Acceso al VPS:** `ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae`. El runner `dhahabi-vps-bp` ya está instalado y corriendo como servicio systemd (`~/actions-runner-bp/`, confirmado `online` en GitHub) — no hace falta tocarlo.

**Checkout local:** `/home/hallen/Dhahabi/BusinessPlaceServer`, rama `fix/delivery-app` (ya contiene `deploy-test.yml` y el resto del trabajo de multi-orden).

---

## Task 1: Escribir el workflow `deploy.yml`

**Files:**
- Create: `BusinessPlaceServer/.github/workflows/deploy.yml`

- [ ] **Step 1: Escribir el workflow**

Crear `/home/hallen/Dhahabi/BusinessPlaceServer/.github/workflows/deploy.yml` con este contenido exacto:

```yaml
name: Deploy Production Stack

on:
  push:
    branches:
      - deploy

jobs:
  deploy:
    runs-on: [self-hosted, dhahabi-vps-bp]
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Verificar que la base de datos de producción esté corriendo
        run: |
          if [ "$(docker inspect -f '{{.State.Running}}' dhahabi-db 2>/dev/null)" != "true" ]; then
            echo "::error::El contenedor dhahabi-db no está corriendo. No se puede desplegar sin la base de datos de producción arriba."
            exit 1
          fi
          echo "dhahabi-db está Up, continuando."

      - name: Reconstruir y desplegar el stack de producción
        run: |
          docker-compose -f docker-compose.yml up -d --build

      - name: Verificar que todos los contenedores de producción quedaron Up
        run: |
          EXPECTED_CONTAINERS="catalogo-command catalogo-query clientes-command clientes-query generales-command generales-query ventas-command ventas-query autenticacion-command autenticacion-query pagos-command pagoscuba-command pagos-query agentes-command agentes-query reportes-query promociones-query"
          FAILED=0
          for c in $EXPECTED_CONTAINERS; do
            STATE=$(docker inspect -f '{{.State.Running}}' "$c" 2>/dev/null || echo "missing")
            if [ "$STATE" != "true" ]; then
              echo "::error::Contenedor $c no está Up (estado: $STATE)"
              FAILED=1
            fi
          done
          if [ "$FAILED" -eq 1 ]; then
            echo "::error::Uno o más contenedores de producción no quedaron Up. Ver 'docker logs <nombre>' en el VPS para la causa. Los servicios que sí se reconstruyeron ya están sirviendo la versión nueva -- no hay rollback automático."
            exit 1
          fi
          echo "Los 17 contenedores de producción están Up."
```

Notar las diferencias deliberadas respecto a `deploy-test.yml`:
- Chequea `dhahabi-db` (no `sqlserver-docker`).
- Corre `docker-compose -f docker-compose.yml up -d --build` directo (no `./build-test.sh`, que no existe una versión de producción de ese script — `docker-compose.yml` ya usa `build: context: .` por servicio, así que un solo comando alcanza).
- **Sin `docker-compose down` antes del `up`** — a propósito, para no apagar los 17 servicios de una sola vez.
- La lista de contenedores esperados no lleva sufijo `test`.

- [ ] **Step 2: Verificar la sintaxis YAML localmente**

```bash
cd /home/hallen/Dhahabi/BusinessPlaceServer
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/deploy.yml'))" && echo "YAML válido"
```

Expected: `YAML válido`

- [ ] **Step 3: Commit**

```bash
cd /home/hallen/Dhahabi/BusinessPlaceServer
git add .github/workflows/deploy.yml
git commit -m "Agregar workflow de CI/CD de producción (rama deploy)"
```

---

## Task 2: Crear la rama `deploy` y disparar el primer deploy real — REQUIERE CONFIRMACIÓN HUMANA EXPLÍCITA

**⚠️ Esta tarea empuja a producción real.** El `git push` de este paso hace que el runner reconstruya y reemplace los 17 contenedores de producción que sirven tráfico real ahora mismo. Ningún agente (subagente ni la sesión principal) ejecuta el comando de push de este paso sin que el usuario lo confirme explícitamente en la conversación, inmediatamente antes de correrlo — no basta con que el plan esté aprobado en general. Si se está ejecutando este plan con subagent-driven-development, el controlador (no el subagente) es quien debe pausar acá y pedir la confirmación antes de despachar esta tarea.

**Pre-requisito:** Task 1 completada y commiteada. Confirmar que `dhahabi-db` está corriendo antes de intentarlo:

```bash
ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae "docker inspect -f '{{.State.Running}}' dhahabi-db"
```

Expected: `true`. Si no, no seguir — reactivar `dhahabi-db` primero (fuera del alcance de este plan, es infraestructura ya existente).

- [ ] **Step 1: (DESPUÉS de confirmación explícita del usuario) Crear y pushear la rama**

```bash
cd /home/hallen/Dhahabi/BusinessPlaceServer
git push origin fix/delivery-app:deploy
```

Expected: `* [new branch] fix/delivery-app -> deploy`. Esto crea la rama `deploy` en GitHub con el workflow ya incluido, lo que dispara el `push` event inmediatamente.

- [ ] **Step 2: Seguir la corrida**

```bash
gh run list --repo ElectronicaUniversal/BusinessPlaceServer --branch deploy --limit 1
```

Copiar el `run-id` de la columna correspondiente y seguirlo:

```bash
gh run watch <run-id> --repo ElectronicaUniversal/BusinessPlaceServer
```

Expected: termina con ✓ (verde). Si falla, revisar el log del step que falló (`gh run view <run-id> --repo ElectronicaUniversal/BusinessPlaceServer --log-failed`) antes de reintentar.

- [ ] **Step 3: Confirmar los 17 contenedores de producción en el VPS**

```bash
ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae "docker ps --format '{{.Names}}\t{{.Status}}' | grep -v test"
```

Expected: los 17 nombres de producción (`catalogo-command`, `catalogo-query`, etc.), todos `Up`.

---

## Task 3: Documentar el flujo de deploy de producción en WORKFLOW.md

**Files:**
- Modify: `/home/hallen/Dhahabi/WORKFLOW.md`

- [ ] **Step 1: Agregar una sección nueva describiendo el flujo, después de "Cómo probar un cambio de backend (BusinessPlaceServer) antes de mergear"**

Agregar este bloque:

```markdown
## Cómo desplegar un cambio de backend (BusinessPlaceServer) a producción

1. Confirmar que lo que se quiere desplegar ya se probó en el stack de test (ver sección anterior).
2. `dhahabi-db` debe estar corriendo en el VPS (`docker inspect -f '{{.State.Running}}' dhahabi-db` — debería estar siempre arriba, es la base de datos de producción).
3. Pushear la rama a desplegar a `deploy`: `git push origin <tu-rama>:deploy` (fuerza el push si `deploy` ya tiene otro contenido: `git push -f origin <tu-rama>:deploy`).
4. Esto dispara el runner `dhahabi-vps-bp` (el mismo que usa `deploy-test`), que reconstruye y reemplaza — servicio por servicio, sin apagar todo el stack — los ~19 microservicios reales sobre `docker-compose.yml` real, contra `dhahabi-db`.
5. **No hay rollback automático.** Si algo sale mal, hay que investigar a mano (`docker logs <contenedor>` en el VPS) y corregir. El backup nocturno automático de `BPDatabase` (cron de `root`, 3am) es la única red de seguridad para la base de datos.
6. **Las migraciones de esquema NO están automatizadas** — si el cambio necesita un `ALTER TABLE` u otro cambio de esquema en `dhahabi-db`, hacerlo a mano por separado antes o después del deploy según corresponda.
7. Runner: `ssh -p 33 -i ~/.ssh/dhahabi dhahabi3@dhahabi.ae 'sudo systemctl status actions.runner.ElectronicaUniversal-BusinessPlaceServer.dhahabi-vps-bp'` para revisar su estado si algo no dispara (mismo runner que `deploy-test`).

**Nota:** los checkouts manuales `~/BP/BusinessPlaceServer` y `/root/BP/BusinessPlaceServer` en el VPS quedan obsoletos desde que este workflow existe — el deploy real ya no depende de hacer `git pull` a mano ahí.
```

- [ ] **Step 2: Guardar** (no requiere commit — `WORKFLOW.md` vive fuera de los tres repos, no está bajo control de versiones)

---

## Self-Review (ya aplicado al escribir este plan)

- **Cobertura de la spec:** trigger por push directo ✓ (Task 2), mismo runner ✓ (Task 1, `runs-on`), sin `down` previo ✓ (Task 1, Step 1), pre-check + post-check ✓ (Task 1, Step 1), sin rollback/sin migraciones/sin backups nuevos/sin aprobación manual ✓ (documentado en Task 3, ningún task los implementa), checkouts manuales no se tocan ✓ (ningún task los modifica).
- **Placeholders:** ninguno — todos los comandos y el YAML están completos.
- **Confirmación humana:** Task 2 la exige explícitamente antes del único paso que toca producción real.
