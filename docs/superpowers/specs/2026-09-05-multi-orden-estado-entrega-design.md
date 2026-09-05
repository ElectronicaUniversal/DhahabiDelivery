# Multi-orden y rediseño de estado de entrega

Repos afectados: `BusinessPlaceServer` (backend, requiere rama de revisión — ver sección Rollout) y `DhahabiDelivery` (cliente).

Resuelve los puntos 2 y 3 del backlog (`WORKFLOW.md`):
- Soporte para múltiples órdenes simultáneas por repartidor.
- Rediseño del estado de la entrega.

## Problema

`Repartidor.Estado` (`BP_AGENTES.REPARTIDOR.ESTADO`, valores `N`/`D`/`E`/`A` en `ConstantesEstadoRepartidor`) es un único campo que mezcla tres conceptos distintos:

1. Si el repartidor está de turno (disponible para que le asignen trabajo).
2. Si tiene algo asignado.
3. Si esa cosa asignada está en camino ahora mismo.

Mientras un repartidor solo podía tener una orden a la vez, estos tres conceptos siempre coincidían y el campo único funcionaba. En cuanto un repartidor tiene 2+ órdenes asignadas, dejan de coincidir: puede estar "de turno" y "en camino con la orden A" y "con la orden B esperando turno" simultáneamente — algo que un solo string no puede representar.

El síntoma concreto ya existe en el código, aunque hoy es inofensivo porque nunca hay dos órdenes en `Procesando` para el mismo repartidor:

- `EstablecerEstadoRepartidorHandlerHandler` (`PresentationLayer/Microservicios/Command/Agentes.Command.Api/Handlers/Repartidor/EstablecerEstadoRepartidorHandlerHandler.cs`) busca "la" orden en estado `Procesando` del repartidor con `repository.BpObtenerUno<Orden>(...)`, que internamente es `FirstOrDefault` (`Repository.cs:20`), no lanza excepción con múltiples resultados. Con 2+ órdenes en `Procesando`, este handler actualiza silenciosamente la **primera que encuentre**, no necesariamente la que el repartidor quiso iniciar/finalizar.
- `AsignarDeliveryHandler` pisa `Repartidor.Estado = ASIGNADO` de forma incondicional al asignar una nueva orden, sin mirar si el repartidor ya tenía una entrega en camino — podría hacer retroceder ese estado visible.
- El lado de lectura (`ObtenerEntregasHandler`) ya devuelve **todas** las órdenes activas del repartidor como lista (`BpObtenerLista<Orden>` filtrando por no `Entregado`/`Cancelado`) — el read-side ya está listo para multi-orden, solo falta corregir el write-side y el cliente.
- El badge de estado en `EntregasItem.razor` (línea 8) está **hardcodeado** como `"Pendiente"` — no refleja ningún dato real.

## Decisiones de negocio (confirmadas con el usuario)

1. **Concurrencia real**: un repartidor puede tener varias órdenes asignadas en cola, pero como máximo **una** con envío `EnCamino` a la vez.
2. **Orden de la cola**: libre elección — el repartidor entra al detalle de cualquier orden asignada y la inicia (no FIFO forzado). Mantiene la UX actual de lista + tap a detalle.
3. **Compatibilidad de despliegue**: se debe mantener compatibilidad con clientes viejos del app que no manden el nuevo parámetro — no se asume un deploy coordinado día-cero de cliente y servidor.

## Diseño

### Principio

La fuente de verdad de "¿qué está pasando con esta entrega?" pasa a ser la **orden** (`Orden.IdEstadoEnvio`, que ya soporta el valor `EnCamino` vía `ConstantesEstadoEnvio.ENCAMINO`, simplemente no se usa hoy en este flujo), no el repartidor. `Repartidor.Estado`/`EstaDisponible` deja de ser el árbitro de negocio para las transiciones y pasa a ser un reflejo de conveniencia que se mantiene actualizado solo para no romper la app vieja.

### Backend (`BusinessPlaceServer`)

**`EstablecerEstadoRepartidorRequest`** (en `Mensajeria`): agrega `int? IdOrden = null`. Al ser opcional, un payload JSON que no lo incluya (cliente viejo) sigue deserializando sin error — no rompe el contrato existente.

**`EstablecerEstadoRepartidorHandlerHandler`**:
- Si `request.IdOrden` viene informado: busca esa orden puntual con `x.Id == request.IdOrden && x.IdRepartidor == repartidor.Id`. Si no existe o no pertenece al repartidor, responde con error (no debe poder tocar la orden de otro).
- Si no viene informado: cae al comportamiento actual sin cambios (`FirstOrDefault` sobre `Procesando`) — es el camino legado, acotado a apps viejas, con la limitación conocida pero sin agravarla.
- Al transicionar a `ENTREGANDO`: valida que el repartidor no tenga **otra** orden (distinta a `IdOrden`) con `IdEstadoEnvio == EnCamino`. Si la hay, responde con un error de conflicto y mensaje explicativo ("ya tienes una entrega en camino, complétala primero").
- Al transicionar a `DISPONIBLE` (finalizar): igual que hoy pero aplicado a la orden puntual identificada por `IdOrden` cuando viene informado.

**`AsignarDeliveryHandler`**: deja de pisar `Repartidor.Estado` incondicionalmente. Nueva regla: solo lo actualiza a `ASIGNADO` si el repartidor no está ya en `ENTREGANDO` (para no hacer retroceder el estado visible de una entrega en curso en apps viejas que solo miran ese campo).

**`EntregaResumen`** (en `Mensajeria`) y **`ObtenerEntregasHandler`**: agregan el estado real de envío (`EstadoEnvio` como string, usando `ConstantesEstadoEnvio`) a cada item de la lista, para que el cliente pueda mostrar el badge correcto y saber cuál (si alguna) ya está en camino.

### Cliente (`DhahabiDelivery`)

**`IRepartidorService`/`RepartidorService`**: `IniciarEntrega`/`FinalizarEntrega` reciben el `IdOrden` puntual (ya se les pasa la `EntregaResumen` completa hoy, así que es tomar `.Id` de ahí) y lo incluyen en el request.

**`EntregasViewModel`**: `EntregaSeleccionada` se mantiene (sigue siendo "cuál estoy mirando en el detalle"), pero la elegibilidad de Iniciar/Finalizar deja de depender de un único `State` global y se calcula mirando: (a) el estado real de la orden seleccionada, (b) si alguna **otra** orden en `EntregasAsignadas` ya está `EnCamino`. Si la hay, el botón Iniciar se deshabilita con un mensaje explicativo en vez de dejar que el backend lo rechace silenciosamente.

**`EntregasItem.razor`**: el badge de estado deja de estar hardcodeado a `"Pendiente"` y usa el campo `EstadoEnvio` real que ahora viaja del backend.

### Manejo de errores

- Intentar iniciar una segunda entrega mientras otra sigue en camino → error explícito del backend, mostrado en el diálogo de error que `MapSection.razor` ya tiene armado (reutiliza el mecanismo existente).
- Cliente viejo sin `IdOrden` → camino legado sin cambios de comportamiento.
- `IdOrden` que no pertenece al repartidor autenticado → rechazado (previene que un repartidor manipule el estado de la orden de otro).

## Testing

Ninguno de los handlers tocados tiene tests hoy. Dado que esta es lógica de negocio con reglas concretas y verificables (una en camino máximo, ownership de la orden, fallback legado sigue igual), se incluyen tests unitarios de estos handlers como parte del plan de implementación, no como opcional:

- `EstablecerEstadoRepartidorHandlerHandler`: con `IdOrden`, sin `IdOrden` (legado), rechazo por segunda orden en camino, rechazo por orden ajena.
- `AsignarDeliveryHandler`: no pisa `Estado` cuando ya está `ENTREGANDO`.
- `ObtenerEntregasHandler`: el `EstadoEnvio` expuesto coincide con el de la orden.

## Rollout (punto 4 del backlog)

El usuario no es el mantenedor principal de `BusinessPlaceServer`. El trabajo de backend va en una rama nueva basada en `origin/dev` (no directo a `dev`) para que el mantenedor real la revise. Ya existe una rama local `fix/delivery-app` (creada 2026-01-26, sin commits, idéntica a `origin/dev`, sin push) que puede usarse para este trabajo en vez de crear una rama adicional.

El cliente (`DhahabiDelivery`) sigue el flujo normal del repo (el usuario sí es mantenedor ahí).
