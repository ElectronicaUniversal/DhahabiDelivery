# CI/CD de producción para BusinessPlaceServer (`deploy`) — Diseño

**Fecha:** 2026-09-06

**Contexto:** Punto 5 del backlog de `WORKFLOW.md`. La mitad de test (`deploy-test`) ya está implementada, validada end-to-end (runner `dhahabi-vps-bp` registrado y activo, workflow probado con un push real que reconstruyó los 17 contenedores del stack de test) y documentada en `docs/superpowers/specs/2026-09-05-deploy-test-cicd-design.md`. Este documento cubre la otra mitad: automatizar el despliegue de producción real.

## Estado actual del despliegue de producción (investigado, no asumido)

Hoy el despliegue de `BusinessPlaceServer` a producción es **100% manual**:

- No existe ningún cron, webhook, systemd timer ni watchtower que dispare deploys.
- Hay dos checkouts idénticos del repo en el VPS, `~/BP/BusinessPlaceServer` y `/root/BP/BusinessPlaceServer`, ambos propiedad de `root`, en la rama `dev-stripe`, en el mismo commit (`7564008`, 2026-08-19 — ~2.5 semanas desactualizado respecto a `origin/dev-stripe`).
- El proceso real es: alguien con acceso root hace `git pull` + `docker compose build` + `docker compose up -d` a mano sobre `docker-compose.yml`.
- `docker-compose.yml` (producción) define cada microservicio con `build: context: . / dockerfile: .../Dockerfile` (construye la imagen directo del Dockerfile, sin pasar por un script intermedio como `build-test.sh`), apunta a `DB_HOST=dhahabi-db` (SQL Server real) y tiene `restart: always`. Usa los puertos de producción (5000–6900 aprox., sin sufijo `test`).
- Existe un backup nocturno automático de `BPDatabase` vía cron de `root` (`1 3 * * *`), como red de seguridad ya presente e independiente de este trabajo.

Este diseño no reemplaza ni compite con ningún mecanismo automatizado existente — no hay ninguno. Automatiza un proceso manual.

## Decisiones de diseño

**Trigger:** push directo a una rama nueva `deploy` en `BusinessPlaceServer`. Sin aprobación manual intermedia, sin relación con `dev-stripe` ni con el flujo de PR/revisión de código del mantenedor del repo — son cosas independientes. Cuando se decide llevar un estado a producción, se hace `git push origin <rama-que-sea>:deploy` y el workflow corre solo, igual que ya funciona hoy para `deploy-test`.

**Runner:** el mismo `dhahabi-vps-bp` (self-hosted, ya registrado y corriendo como servicio systemd en el VPS) que ejecuta `deploy-test`. No se registra un runner nuevo — ambos workflows corren en la misma máquina, mismo Docker daemon, misma red, así que separar runners no da aislamiento real.

**Downtime:** a diferencia de `build-test.sh` (que hace `docker-compose down` + `up -d --force-recreate`, aceptable en test porque no importa apagar todo el stack), el workflow de producción usa `docker compose -f docker-compose.yml up -d --build` **sin `down` previo**. Compose reconstruye y reemplaza solo los servicios cuyo código cambió, uno por uno — downtime de segundos por servicio en vez de un apagón total del stack de ~19 microservicios.

**Manejo de errores:** igual que `deploy-test` — sin rollback automático. Si el post-check falla, el job de GitHub Actions queda en rojo, visible, y hay que investigar/arreglar a mano. Los servicios que sí se reconstruyeron bien quedan sirviendo la versión nueva; los que fallaron no se revierten solos.

**Migraciones de base de datos:** fuera de alcance. Si un cambio de código necesita un `ALTER TABLE` u otro cambio de esquema en `dhahabi-db`, se hace a mano por separado, igual que hoy. El repo no mantiene migraciones EF Core al día (solo existe una migración vieja de 2024), así que automatizar esto implicaría resolver esa deuda técnica primero — no es parte de este trabajo.

**Backups:** no se agrega ningún backup nuevo disparado por el deploy. El backup nocturno automático ya existente (cron de `root`, 3am) sigue siendo la única red de seguridad.

**Checkouts manuales existentes:** `~/BP/BusinessPlaceServer` y `/root/BP/BusinessPlaceServer` quedan sin uso una vez que este workflow esté funcionando — el checkout que importa a partir de ahora es el que crea `actions/checkout` dentro del workspace del runner (`~/actions-runner-bp/_work/...`), igual que ya pasa con `deploy-test`. No se borran ni se tocan como parte de este trabajo; solo dejan de ser la fuente de verdad del deploy.

## Pasos del workflow (`deploy.yml`)

1. **Trigger:** `on: push: branches: ["deploy"]`, `runs-on: [self-hosted, dhahabi-vps-bp]`.
2. **Checkout:** `actions/checkout@v4`, en el workspace del runner (no toca los checkouts manuales existentes).
3. **Pre-check:** confirmar que el contenedor `dhahabi-db` está `Up` (`docker ps` + grep, mismo patrón que el pre-check de `sqlserver-docker` en `deploy-test.yml`). Si no está corriendo, falla el job con un mensaje claro en vez de intentar construir contra una base de datos que no existe.
4. **Deploy:** `docker compose -f docker-compose.yml up -d --build` desde la raíz del checkout.
5. **Post-check:** `docker compose -f docker-compose.yml ps` y confirmar que la lista completa de contenedores de producción (mismos ~19 nombres ya confirmados corriendo hoy: `catalogo-command`, `catalogo-query`, `clientes-command`, `clientes-query`, `generales-command`, `generales-query`, `ventas-command`, `ventas-query`, `autenticacion-command`, `autenticacion-query`, `pagos-command`, `pagoscuba-command`, `pagos-query`, `agentes-command`, `agentes-query`, `reportes-query`, `promociones-query`, sin sufijo `test`) están `Up`. Si alguno falta, falla el job.

## Fuera de alcance (explícito)

- Rollback automático.
- Migraciones de esquema automatizadas.
- Backups adicionales disparados por el deploy.
- Aprobación manual / GitHub Environments protegidos.
- Cualquier cambio al flujo de PR/revisión de código del mantenedor del repo — este trabajo es infraestructura de deploy, no toca cómo se aprueba código.
- Limpieza de los checkouts manuales existentes en el VPS.
