# ADR-0007: Inyección de dependencias y ciclos de vida

- **Estado:** Aceptado
- **Fecha:** 2026-10-27
- **Responsable(s):** M01

## Contexto
La app se compone de varias capas: datos (SQLite), red (API), autenticación, sincronización, reserva y presentación.
Cada capa tiene un responsable distinto, así que necesitamos:
- que ninguna clase cree sus dependencias con `new`, para poder sustituirlas en las pruebas,
- decidir cuánto vive cada objeto, para no duplicar conexiones ni compartir estado de pantalla por error,
- que diez personas puedan registrar sus servicios sin generar conflictos de Git en `MauiProgram.cs`.

MAUI trae el contenedor de `Microsoft.Extensions.DependencyInjection`, con tres ciclos de vida: Singleton, Transient y
Scoped. En MAUI no hay un *scope* natural (como la petición HTTP en ASP.NET), así que Scoped se comporta como Singleton
y confunde.

## Decisión
- Usamos **el contenedor integrado de MAUI** con **inyección por constructor** y dependemos siempre de interfaces
  (`INavigationService`, `IBarberTurnoApi`, `ICitaRepository`, ...).
- **Cada capa se registra en su propio archivo `*Registration.cs`** con un método de extensión (`AddDataLayer`,
  `AddApiLayer`, `AddAuthLayer`, `AddSyncLayer`, `AddBookingLayer`). `MauiProgram.cs` (M01) solo los encadena y
  registra la capa de presentación (`AddPresentationLayer`).
- Ciclos de vida:

| Tipo | Ciclo de vida | Motivo |
|---|---|---|
| `AppDatabase`, repositorios | Singleton | Una sola conexión SQLite asíncrona para toda la app; abrir varias provoca bloqueos. |
| `IBarberTurnoApi` (HttpClient tipado) | Fábrica de `HttpClient` (`AddHttpClient`) | Reutiliza las conexiones y evita agotar los sockets. |
| `ISessionStorage`, `IAuthService`, `IReminderService` | Singleton | La sesión es única y global. |
| `INetworkService`, `ISyncService` | Singleton | Escuchan cambios de conectividad durante toda la vida de la app. |
| `IAvailabilityService`, `IBookingService` | Singleton | No guardan estado; delegan en la API y en los repositorios. |
| `INavigationService`, `IDialogService` | Singleton | No guardan estado; envuelven `Shell.Current`. |
| ViewModels y páginas | Transient | Cada navegación crea una pantalla nueva y limpia, sin datos de la visita anterior. |

- **No usamos Scoped.**
- Un Singleton nunca recibe un Transient por constructor (eso se conoce como *captive dependency*).

## Alternativas consideradas
- **Elegida: contenedor integrado y un archivo de registro por capa.** No necesita paquetes extra, es el estándar de .NET
  (el mismo que usa la API en ASP.NET Core) y Shell lo usa para crear las páginas. Con un archivo por capa, cada
  integrante edita solo el suyo.
- **Todo registrado directamente en `MauiProgram.cs`:** funciona igual, pero los diez integrantes editarían el mismo
  archivo y habría conflictos en casi todos los Pull Request.
- **Contenedor de terceros (Autofac, DryIoc):** ofrece registro por convención y módulos, pero añade una dependencia y
  conceptos nuevos que una app de este tamaño no necesita.
- **Service Locator o `new` directo** (`App.Current.Handler.MauiContext.Services`, singletons estáticos): esconde las
  dependencias, impide sustituirlas en las pruebas y acopla las clases entre sí.
- **ViewModels Singleton:** conservarían datos al volver a una pantalla, pero mostrarían información vieja (por ejemplo,
  la reserva anterior en Confirmar reserva) y compartirían estado entre los dos `PerfilPage`.

## Consecuencias
- **Ventajas:** las dependencias se ven en cada constructor, se pueden sustituir con dobles en las pruebas, hay una sola
  conexión a SQLite y cada responsable registra lo suyo sin conflictos.
- **Costos:** cada clase nueva hay que registrarla, porque si no, falla al resolverse en tiempo de ejecución, no al
  compilar. Recrear los ViewModels Transient vuelve a cargar los datos en cada visita.
- **Riesgos y mitigación:**
  - *Olvidar un registro.* Cada `*Registration.cs` tiene su `TODO`, y en cada Pull Request probamos que la pantalla
    afectada abre sin errores.
  - *Captive dependency, o estado compartido en un Singleton.* Lo revisamos en los Pull Request. Los servicios Singleton
    no guardan estado de pantalla.
  - *Cargas repetidas por ser Transient.* Si hiciera falta, se cachean los datos en el servicio o en el repositorio
    (que sí es Singleton), nunca en el ViewModel.
