# ADR-0009: Recordatorios con notificaciones locales

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M06

## Contexto
El cliente debe recibir un aviso en el teléfono antes de su cita, con el permiso correspondiente. Hay que decidir cómo se programa, qué pasa si la cita se cancela, se rechaza o cambia, y qué ocurre con el permiso en Android 13+ (`POST_NOTIFICATIONS`).

## Decisión
1. Se usa **`Plugin.LocalNotification` 14.1** (`UseLocalNotification()` en `MauiProgram`), con notificaciones **locales programadas** (no hay servidor de notificaciones).
2. `ReminderService` (implementa `IReminderService`, singleton) avisa **60 minutos antes** de la cita; si ya falta menos, avisa en unos segundos; si la cita ya empezó, no avisa.
3. El **id** de la notificación se deriva del Guid de la cita (`IdNotificacion`), de modo que se puede cancelar o reemplazar sin guardar nada más.
4. **Permiso:** `ProgramarAsync` consulta y, si falta, **solicita** el permiso de notificaciones en el momento de reservar (contexto claro para el usuario). Sin permiso la reserva sigue siendo válida; solo no se programa el aviso.
5. **Cuándo se programa:** al confirmar la reserva, al guardar una reserva pendiente sin conexión y cuando una pendiente se confirma al sincronizar. **Cuándo se cancela:** al cancelar la cita, al ser rechazada (`409` al sincronizar) y, en la reconciliación, cuando el servidor la marca cancelada.
6. **Reconciliación:** tras sincronizar las citas, `ReconciliarAsync` recorre las citas del cliente y programa las vigentes y cancela las que ya no corresponden, así los recordatorios se corrigen aunque la cita cambie desde otro dispositivo.
7. Cualquier fallo del plugin se registra y se ignora: un recordatorio nunca debe impedir una reserva.

## Alternativas consideradas
- **Notificaciones push (FCM/APNs):** funcionan con la app cerrada y desde el servidor, pero exigen cuenta de Firebase, un servicio emisor y gestión de tokens; desproporcionado y no pedido.
- **`AlarmManager`/`UNUserNotificationCenter` nativos directos:** control total, pero código distinto por plataforma; el plugin los envuelve.
- **Tareas en segundo plano periódicas:** Android las limita (ahorro de batería) y no son puntuales.

## Consecuencias
- **Ventajas:** funciona sin conexión, sin infraestructura extra; el aviso sigue la vida de la cita (cancelar/rechazar lo elimina).
- **Costos:** en Android 12+ las alarmas exactas requieren permiso especial; si el sistema lo niega, el aviso puede llegar con algo de retraso. Si el usuario borra los datos de la app o cambia de teléfono, los avisos locales se pierden hasta la siguiente sincronización (la reconciliación los recrea).
- **Riesgos y mitigación:** en Windows sin empaquetar (MSIX) las notificaciones pueden no mostrarse; el entorno objetivo es el emulador/teléfono Android. Aún no existe la pantalla de **reprogramar** cita; `ReprogramarAsync` ya cancela y reprograma el aviso para cuando se implemente.
