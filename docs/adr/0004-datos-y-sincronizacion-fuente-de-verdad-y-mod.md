# ADR-0004: Estrategia de datos y sincronización (fuente de verdad y modo sin conexión)

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M07 (Jorge Cabrera)

## Contexto
El cliente y el barbero deben poder consultar sus datos sin señal, y una reserva hecha sin conexión debe quedar pendiente de confirmación. Pero la disponibilidad real depende de lo que reservaron otras personas, que el teléfono no puede ver sin red. Preguntas a resolver: ¿quién es la autoridad sobre las horas libres?, ¿qué pasa con una reserva sin conexión si la hora ya fue ocupada?, ¿cómo se representan las horas libres?

## Decisión
1. **El servidor es la autoridad** sobre disponibilidad y sobre la confirmación de citas. SQLite es una **caché** de lectura y una **cola** de reservas pendientes.
2. **Lectura:** `SyncService` descarga catálogos (barberos, servicios) y citas del usuario al abrir y cada vez que vuelve la red (`INetworkService.ConexionCambio`). Los ViewModels leen siempre de los repositorios locales.
3. **Horas libres:** `AvailabilityService` pregunta primero a la API (`/disponibilidad`, franjas cada 30 min según la duración del servicio y las citas existentes). Si no hay conexión, **calcula localmente** con el horario y los días del barbero, la duración y las citas guardadas (`ExisteSolapamientoAsync`).
4. **Reserva sin conexión:** se guarda una `Cita` con `Estado = Pendiente`, `PendienteSincronizar = true`, a nombre del cliente de la sesión y con la duración real del servicio.
5. **Al volver la red**, `SincronizarPendientesAsync` envía cada pendiente (el `Id` Guid del cliente hace la operación **idempotente**). Si el servidor responde `409`, la cita pasa a `Rechazada`, se cancela su recordatorio y `SyncService` lanza el evento `ReservaRechazada`; `MisCitas` muestra el aviso al cliente. Si se acepta, pasa a `Confirmada`.
6. Cada sincronización usa un `SemaphoreSlim` no reentrante para evitar ejecuciones simultáneas.

## Alternativas consideradas
- **Dispositivo como autoridad (reservar siempre en local y subir después):** funciona sin red, pero dos teléfonos podrían aceptar la misma hora y habría que deshacer citas ya "confirmadas"; incompatible con la regla central.
- **Solo en línea (sin caché):** simple y sin conflictos, pero incumple el requisito de uso sin conexión.
- **Fusión automática (CRDT / última escritura gana):** excesivo para un recurso escaso como una hora; la regla de negocio exige un ganador único, no una mezcla.

## Consecuencias
- **Ventajas:** nunca se confirma una hora sin que el servidor la haya aceptado; el usuario siempre ve algo, incluso sin señal.
- **Costos:** una hora ofrecida sin conexión puede resultar ocupada: el cliente verá la cita como "pendiente" y luego "rechazada". Eso es inherente a reservar sin ver a los demás; se comunica con claridad en vez de ocultarlo.
- **Riesgos y mitigación:** el cálculo local puede ofrecer horas desactualizadas (se documenta en pantalla con el aviso de "sin conexión"); la agenda del barbero solo incluye lo sincronizado. Si el cliente nunca recupera conexión la cita queda pendiente: la lista la marca como tal.
