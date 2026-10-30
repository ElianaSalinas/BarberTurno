# ADR-0007: Inyección de dependencias y ciclos de vida

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M01 (Eliana)

## Contexto
Con MVVM y capas, cada clase necesita colaboradores (repositorios, API, sesión, navegación). Había que decidir cómo se obtienen, cuánto viven y qué comparte estado. Un error de ciclo de vida típico: dos instancias de la sesión (el login guarda el usuario en una y la pantalla de agenda lee otra vacía).

## Decisión
1. Se usa el contenedor de **`Microsoft.Extensions.DependencyInjection`** de MAUI. `MauiProgram` solo encadena un método de registro por capa (`AddDataLayer`, `AddApiLayer`, `AddAuthLayer`, `AddSyncLayer`, `AddNotificationLayer`, `AddBookingLayer`, `AddPresentationLayer`).
2. **Singleton** para lo que tiene estado compartido o un recurso costoso: `IAppDatabase` y repositorios (una conexión), `HttpClient`/`IBarberTurnoApi` (con el token), `IAuthService` (guarda `SesionActual`), `ISessionStorage`, `INetworkService`, `ISyncService`, `INavigationService`, `IDialogService`, `IReminderService`.
3. **Transient** para lo que no debe conservar estado entre usos: todos los ViewModels y páginas, `IBookingService` e `IAvailabilityService`. Así cada pantalla nace limpia.
4. Las páginas y los ViewModels reciben todo por **constructor**; no hay localizadores estáticos. Shell crea las páginas mediante `ContentTemplate`, lo que permite inyectarlas.
5. Regla: un singleton **no** depende de un transient. Para casos opcionales (recordatorios en `BookingService`/`SyncService`) se usan parámetros con valor por defecto, que el contenedor rellena si existen.

## Alternativas consideradas
- **Localizador de servicios / estáticos:** cómodo, pero oculta dependencias y no se puede sustituir en pruebas.
- **Crear las dependencias a mano (`new`):** sin magia, pero acopla las capas y duplica instancias de la conexión y de la sesión.
- **Todo singleton:** evita instanciar, pero los ViewModels conservarían estado de la pantalla anterior (listas, errores) y habría que limpiarlos a mano.
- **Ámbitos (*scoped*) por página:** MAUI no crea un ámbito por página; sería un mecanismo extra sin beneficio claro aquí.

## Consecuencias
- **Ventajas:** dependencias visibles en el constructor, pruebas con Moq sin tocar la aplicación, una sola sesión y una sola conexión.
- **Costos:** los singletons con estado (`SesionActual`) deben **limpiarse al cerrar sesión** (`LogoutAsync`); un olvido filtraría datos de un usuario al siguiente. El encabezado `Authorization` del `HttpClient` singleton es mutable.
- **Riesgos y mitigación:** una dependencia circular rompe el arranque; se evitó haciendo que `SyncService` y `BookingService` dependan de `IReminderService` y no al revés.
