# Decisiones de arquitectura (ADR)

Este documento reúne **todos los registros de decisión de arquitectura** de BarberTurno en un solo lugar.
Cada decisión sigue la estructura de la práctica: contexto, decisión, alternativas consideradas y consecuencias
(incluidos los costos). Los archivos individuales siguen disponibles en [`docs/adr/`](adr/).

Documentos relacionados: [Arquitectura](01-arquitectura.md) · [Pantallas y flujos](03-pantallas.md).

## Índice

| ADR | Decisión | Exigido por el PDF | Estado | Fecha | Responsable(s) |
|---|---|---|---|---|---|
| [ADR-0001](#adr-0001-patrón-de-presentación-mvvm-y-su-justificación) | Patrón de presentación (MVVM) y su justificación | Sí (#1) | Aceptado | 2026-10-30 | M01 (Eliana) |
| [ADR-0002](#adr-0002-estrategia-de-navegación-entre-los-perfiles-de-cliente-y-barbero) | Estrategia de navegación entre los perfiles de cliente y barbero | Sí (#2) | Aceptado | 2026-10-30 | M01 (Eliana) |
| [ADR-0003](#adr-0003-persistencia-local-con-sqlite) | Persistencia local con SQLite | Sí (#3) | Aceptado | 2026-10-30 | M02 (Brailyn) |
| [ADR-0004](#adr-0004-estrategia-de-datos-y-sincronización-fuente-de-verdad-y-modo-sin-conexión) | Estrategia de datos y sincronización (fuente de verdad y modo sin conexión) | Sí (#4) | Aceptado | 2026-10-30 | M07 (Jorge Cabrera) |
| [ADR-0005](#adr-0005-capa-de-red-y-traducción-de-datos) | Capa de red y traducción de datos | Sí (#5) | Aceptado | 2026-10-27 | Módulo 6 (Cliente de API y Hosting) y Módulo 2 (Modelos y DTOs) |
| [ADR-0006](#adr-0006-autenticación-y-manejo-de-la-sesión) | Autenticación y manejo de la sesión | Sí (#6) | Aceptado | 2026-10-28 | M5 (Autenticación, roles y sesión) |
| [ADR-0007](#adr-0007-inyección-de-dependencias-y-ciclos-de-vida) | Inyección de dependencias y ciclos de vida | Sí (#7) | Aceptado | 2026-10-30 | M01 (Eliana) |
| [ADR-0008](#adr-0008-cómo-se-evita-que-dos-citas-queden-a-la-misma-hora) | Cómo se evita que dos citas queden a la misma hora | Sí (#8) | Aceptado | 2026-10-30 | M04 (Jeifferson) y M08 |
| [ADR-0009](#adr-0009-recordatorios-con-notificaciones-locales) | Recordatorios con notificaciones locales | Recomendado | Aceptado | 2026-10-30 | M06 |
| [ADR-0010](#adr-0010-manejo-de-estados-de-interfaz-y-errores) | Manejo de estados de interfaz y errores | Recomendado | Aceptado | 2026-10-30 | M09 (Albert Frank) |
| [ADR-0011](#adr-0011-tema-visual-claro-y-oscuro) | Tema visual claro y oscuro | Recomendado | Aceptado | 2026-10-30 | M10 (Equipo) |
| [ADR-0012](#adr-0012-configuración-y-secretos-hosting-de-la-api) | Configuración y secretos; hosting de la API | Recomendado | Aceptado | 2026-10-27 | Módulo 6 (Cliente de API y Hosting) |

## Preguntas de la defensa y dónde se responden

| Pregunta que guía la práctica | Dónde se responde |
|---|---|
| ¿Cómo se evita que dos clientes reserven la misma hora casi a la vez? ¿Basta con comprobar en el teléfono? | ADR-0008 |
| ¿Quién es la autoridad sobre las horas libres: el dispositivo o el servidor? | ADR-0004 y ADR-0008 |
| ¿Qué pasa con una reserva sin conexión si la hora ya fue ocupada? ¿Cómo se le comunica al cliente? | ADR-0004 |
| ¿Cómo se representan las horas libres a partir de la duración del servicio y de las citas existentes? | ADR-0004 |
| ¿Dónde se guarda la credencial del usuario y por qué ahí? | ADR-0006 |
| ¿Cómo se programa el recordatorio y qué pasa si la cita se cancela o se reprograma? | ADR-0009 |
| Si la API cambiara el nombre de un campo, ¿qué archivos habría que modificar? | ADR-0005 |
| ¿Por qué se eligió SQLite y qué se consideró como alternativa? | ADR-0003 |

---

## ADR-0001: Patrón de presentación (MVVM) y su justificación

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M01 (Eliana)

### Contexto
BarberTurno tiene unas 15 pantallas con estados de carga/vacío/error, formularios con validación y datos que llegan de SQLite y de una API. En .NET MAUI el enlace de datos de XAML es la forma natural de conectar la interfaz con la lógica. La práctica exige MVVM con las herramientas de la comunidad, inyección de dependencias y **ausencia de lógica de negocio en el code-behind**. Queríamos que la lógica de cada pantalla se pudiera probar sin levantar la interfaz (el PDF pide al menos una prueba de ViewModel con un repositorio simulado).

### Decisión
1. Se usa **MVVM** con `CommunityToolkit.Mvvm` 8.4: `[ObservableProperty]` y `[RelayCommand]` generan propiedades y comandos en tiempo de compilación.
2. Todos los ViewModels heredan de `BaseViewModel` (`State`, `MensajeError`, `IsBusy`, `OnAppearing/OnDisappearing`), de modo que cada pantalla expone el mismo vocabulario de estados.
3. Las vistas declaran `x:DataType` (enlaces compilados) y el proyecto usa `MauiXamlInflator=SourceGen`: los errores de enlace se detectan al compilar.
4. El **code-behind solo** asigna el `BindingContext` y reenvía `OnAppearing` al ViewModel. Las dependencias llegan por constructor (ver ADR-0007).
5. Capas: `Models` → `Data` (repositorios) → `Services` (API, sincronización, reserva, notificaciones) → `ViewModels` → `Views`. Un ViewModel nunca conoce a una vista.

### Alternativas consideradas
- **Code-behind con eventos (sin patrón):** rápido para una pantalla, pero la lógica queda atada a controles y no se puede probar; descartado por el requisito de la práctica y porque triplica el trabajo al cambiar el diseño.
- **MVU / Comet / MauiReactor:** modelo unidireccional atractivo, pero sale del conjunto XAML + herramientas de la comunidad que pide el curso y el equipo no lo conocía.
- **ReactiveUI / Prism:** más potentes (enrutamiento, regiones), pero añaden dependencias y una curva de aprendizaje que no se justifica para 15 pantallas.

### Consecuencias
- **Ventajas:** lógica probada con Moq sin interfaz (`ViewModelMockTests`), pantallas coherentes, menos código repetitivo gracias a los generadores.
- **Costos:** los generadores esconden código (hay que saber que `CargarAsync` produce `CargarCommand`); los enlaces compilados obligan a declarar `x:DataType` en cada plantilla.
- **Riesgos y mitigación:** la disciplina "sin lógica en code-behind" depende del equipo. Se revisa en cada integración. Algunos manejadores de toque (campana de avisos, "Ver calendario") viven en el code-behind pero solo delegan en un ayudante (`AvisosHelper`) o navegan; si crecen, deben pasar a un comando.

---

## ADR-0002: Estrategia de navegación entre los perfiles de cliente y barbero

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M01 (Eliana)

### Contexto
La misma aplicación sirve a dos perfiles con pantallas distintas: el cliente (Inicio, Reservar, Mis turnos, Servicios, Perfil) y el barbero (Mi agenda, Calendario, Servicios, Clientes, Perfil). Además hay un flujo de inicio de sesión previo y pantallas apiladas (elegir servicio → hora → confirmar). Había que decidir cómo se separan los perfiles, cómo se llega a cada uno y cómo se evita que un perfil vea las pantallas del otro.

### Decisión
1. Se usa **Shell** con **tres `TabBar` raíz**: `auth` (login, sin barra de pestañas), `cliente` y `barbero`. Cada `ShellContent` crea su página con `ContentTemplate`, así que la página se resuelve desde el contenedor de dependencias.
2. Tras el login, `LoginViewModel` navega con rutas absolutas según el rol (`//cliente`, `//barbero`) y el cierre de sesión vuelve a `//login`. `NavigationService.NormalizarRuta` traduce esos atajos a la ruta canónica (`//cliente/inicio`, `//barbero/agenda`, `//auth/login`).
3. Las pantallas que se apilan (servicios, selección de hora, confirmar reserva, registro, detalle de cita) se registran en `AppShell.xaml.cs` con **constantes** de `Routes`, sin cadenas sueltas.
4. Toda navegación pasa por `INavigationService`, que descarta dobles toques con un semáforo y aplica un tiempo máximo de 5 s para no dejar la interfaz bloqueada. Los datos entre pantallas viajan como parámetros de Shell (`IQueryAttributable`, p. ej. `citaId`).
5. El arranque muestra primero una pantalla de presentación animada y luego sustituye la página de la ventana por `AppShell`.

### Alternativas consideradas
- **Dos `AppShell` distintos (uno por rol) reemplazados en la ventana:** aísla del todo los perfiles, pero duplica la configuración (colores, rutas) y complica las transiciones; descartado.
- **`NavigationPage` con `PushAsync` manual:** control total, pero hay que implementar a mano las pestañas, el botón atrás y el estado por pestaña; Shell ya los trae.
- **Flyout (menú lateral):** menos natural en móvil para 4-5 destinos de uso frecuente que las pestañas inferiores.

### Consecuencias
- **Ventajas:** una sola fuente de rutas, pestañas nativas por perfil, navegación testeable (se simula `INavigationService`).
- **Costos:** los dos perfiles viven en el mismo Shell; las páginas del otro perfil existen como definición aunque no se creen hasta usarse. La separación depende de **no navegar** a rutas del otro rol, no de un bloqueo técnico (la API sí protege los datos por rol/token).
- **Riesgos y mitigación:** las rutas relativas dependen de la pestaña actual; por eso las pantallas apiladas se abren con nombres registrados y las raíces con rutas absolutas normalizadas.

---

## ADR-0003: Persistencia local con SQLite

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M02 (Brailyn)

### Contexto
La aplicación debe recordar sus datos entre sesiones y permitir consultar barberos, servicios y citas sin señal. Las entidades obligatorias son Cliente, Barbero, Servicio y Cita (más la relación barbero-servicio). Se necesitaba un almacenamiento relacional, consultable por rangos de fecha (agenda del día, historial) y capaz de hacer cumplir restricciones únicas (una cita activa por barbero y hora de inicio).

### Decisión
1. **SQLite** con `sqlite-net-pcl` 1.11 y `SQLitePCLRaw.bundle_green`. Tablas: `Cliente`, `Barbero`, `Servicio`, `BarberoServicio` y `Cita`.
2. Una única conexión asíncrona compartida (`AppDatabase`, singleton), creada de forma diferida y protegida con `SemaphoreSlim`; se activa el modo **WAL** para leer mientras la sincronización escribe.
3. Esquema: `CreateTablesAsync` crea las tablas y añade columnas nuevas; los **índices** que los atributos no permiten (el parcial de citas activas y el único de barbero-servicio) se crean con SQL en una migración idempotente (`CREATE UNIQUE INDEX IF NOT EXISTS`) con versión de esquema.
4. Acceso solo mediante **repositorios** (`ICitaRepository`, etc., singletons), nunca con la conexión desde un ViewModel.
5. Fechas con `storeDateTimeAsTicks`. Convención: **`Cita.Inicio` y `Fin` se guardan en hora local**; el servidor y los DTO usan UTC y `DtoMapper` convierte en ambos sentidos.

### Alternativas consideradas
- **EF Core + SQLite:** migraciones y LINQ más completos, pero mayor tamaño, arranque más lento y fricción con recorte (*trimming*) en móvil; el esquema es pequeño.
- **LiteDB / Realm:** documentos u objetos sin SQL; no ofrecen índices únicos parciales ni consultas relacionales con la misma facilidad y el equipo ya conoce SQL.
- **`Preferences`/archivos JSON:** sirven para ajustes, no para consultar por rangos ni imponer unicidad.

### Consecuencias
- **Ventajas:** funciona sin conexión, restricción única local, consultas rápidas, dependencia pequeña.
- **Costos:** sqlite-net no tiene migraciones reales; los cambios de esquema se escriben a mano. Al guardar fechas como *ticks* se pierde el `Kind`: al leer, `DateTime` llega como `Unspecified`, por lo que **no se debe llamar `ToLocalTime()` sobre `Cita.Inicio`** (error que ya ocurrió en "Mis citas" y se corrigió).
- **Riesgos y mitigación:** una sola conexión serializa escrituras; el modo WAL y las consultas cortas lo mantienen fluido. El catálogo de prueba solo se siembra con la base vacía y la sincronización lo reemplaza.

---

## ADR-0004: Estrategia de datos y sincronización (fuente de verdad y modo sin conexión)

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M07 (Jorge Cabrera)

### Contexto
El cliente y el barbero deben poder consultar sus datos sin señal, y una reserva hecha sin conexión debe quedar pendiente de confirmación. Pero la disponibilidad real depende de lo que reservaron otras personas, que el teléfono no puede ver sin red. Preguntas a resolver: ¿quién es la autoridad sobre las horas libres?, ¿qué pasa con una reserva sin conexión si la hora ya fue ocupada?, ¿cómo se representan las horas libres?

### Decisión
1. **El servidor es la autoridad** sobre disponibilidad y sobre la confirmación de citas. SQLite es una **caché** de lectura y una **cola** de reservas pendientes.
2. **Lectura:** `SyncService` descarga catálogos (barberos, servicios) y citas del usuario al abrir y cada vez que vuelve la red (`INetworkService.ConexionCambio`). Los ViewModels leen siempre de los repositorios locales.
3. **Horas libres:** `AvailabilityService` pregunta primero a la API (`/disponibilidad`, franjas cada 30 min según la duración del servicio y las citas existentes). Si no hay conexión, **calcula localmente** con el horario y los días del barbero, la duración y las citas guardadas (`ExisteSolapamientoAsync`).
4. **Reserva sin conexión:** se guarda una `Cita` con `Estado = Pendiente`, `PendienteSincronizar = true`, a nombre del cliente de la sesión y con la duración real del servicio.
5. **Al volver la red**, `SincronizarPendientesAsync` envía cada pendiente (el `Id` Guid del cliente hace la operación **idempotente**). Si el servidor responde `409`, la cita pasa a `Rechazada`, se cancela su recordatorio y `SyncService` lanza el evento `ReservaRechazada`; `MisCitas` muestra el aviso al cliente. Si se acepta, pasa a `Confirmada`.
6. Cada sincronización usa un `SemaphoreSlim` no reentrante para evitar ejecuciones simultáneas.

### Alternativas consideradas
- **Dispositivo como autoridad (reservar siempre en local y subir después):** funciona sin red, pero dos teléfonos podrían aceptar la misma hora y habría que deshacer citas ya "confirmadas"; incompatible con la regla central.
- **Solo en línea (sin caché):** simple y sin conflictos, pero incumple el requisito de uso sin conexión.
- **Fusión automática (CRDT / última escritura gana):** excesivo para un recurso escaso como una hora; la regla de negocio exige un ganador único, no una mezcla.

### Consecuencias
- **Ventajas:** nunca se confirma una hora sin que el servidor la haya aceptado; el usuario siempre ve algo, incluso sin señal.
- **Costos:** una hora ofrecida sin conexión puede resultar ocupada: el cliente verá la cita como "pendiente" y luego "rechazada". Eso es inherente a reservar sin ver a los demás; se comunica con claridad en vez de ocultarlo.
- **Riesgos y mitigación:** el cálculo local puede ofrecer horas desactualizadas (se documenta en pantalla con el aviso de "sin conexión"); la agenda del barbero solo incluye lo sincronizado. Si el cliente nunca recupera conexión la cita queda pendiente: la lista la marca como tal.

---

## ADR-0005: Capa de red y traducción de datos

- **Estado:** Aceptado
- **Fecha:** 2026-10-27
- **Responsable(s):** Módulo 6 (Cliente de API y Hosting) y Módulo 2 (Modelos y DTOs)

### Contexto
La aplicación móvil *BarberTurno* desarrollada en .NET MAUI requiere comunicarse de forma asíncrona y segura con un backend centralizado en ASP.NET Core para autenticación, obtención del catálogo de barberos y servicios, consulta de franjas horarias disponibles y creación/cancelación de reservas.

En entornos móviles, las conexiones de red son inestables, intermitentes y sujetas a latencia o caídas abruptas. Además, el backend puede rechazar operaciones por razones de negocio esperadas (por ejemplo, código `409 Conflict` cuando otro usuario reserva la misma hora simultáneamente).

Era necesario responder a:
1. ¿Cómo estructurar las llamadas HTTP y gestionar el ciclo de vida del cliente de red?
2. ¿Cómo propagar los resultados y fallos a los ViewModels sin lanzar excepciones descontroladas?
3. ¿Cómo independizar las entidades internas de base de datos respecto a los contratos de intercambio?
4. ¿Qué archivos deben modificarse si la API cambia o renombra un campo?

### Decisión

1. **Cliente HTTP Tipado con `IBarberTurnoApi` e Inyección de Dependencias:**
   - Se definió la interfaz `IBarberTurnoApi` y su implementación concreta `BarberTurnoApi`, abstrayendo el uso de `HttpClient`.
   - Se inyecta como servicio Singleton en `ApiServiceRegistration.cs`, configurando un tiempo de espera (*timeout*) de 15 segundos y la dirección base (`BaseAddress`) parametrizada mediante `AppConstants.ApiBaseUrl`.

2. **Mapeo de Respuestas con el Contenedor `ApiResult<T>`:**
   - Toda llamada a la API retorna `Task<ApiResult<T>>`.
   - `ApiResult<T>` expone: `Exito` (bool), `Datos` (T?), `CodigoHttp` (int?), `SinConexion` (bool) y `Error` (string?).
   - Se capturan controladamente las excepciones de red (`HttpRequestException`, `TaskCanceledException`) traduciéndolas a `SinConexion = true` y mensajes amigables para el usuario.
   - Respuestas HTTP como `409 Conflict` o `401 Unauthorized` se devuelven con `Exito = false` y su código correspondiente, permitiendo que la lógica de presentación (ViewModels) actúe sin bloques `try-catch` dispersos.

3. **Separación Estricta entre DTOs y Entidades de Dominio:**
   - La API intercambia exclusivamente objetos de transferencia de datos (**DTOs** ubicados en `BarberTurno/Dtos/Dtos.cs` y `BarberTurno.Api/Dtos/ApiDtos.cs`).
   - Ni las entidades de SQLite local (`Models/`) ni las de EF Core del servidor (`Domain/`) son expuestas a través de la red.
   - Si la API renombra o añade un campo, **únicamente se modifican los archivos `Dtos.cs` y los mappers correspondientes**, dejando intacto el resto de la aplicación.

4. **Gestión del Token de Autenticación:**
   - `BarberTurnoApi` dispone del método `SetAuthToken(string? token)`, que actualiza el encabezado `Authorization: Bearer <token>` para las peticiones subsecuentes tras el login exitoso (gestionado por Módulo 5).

### Alternativas consideradas

- **Refit / RestEase (Generación automática de clientes HTTP):**
  - *Ventaja:* Menos código manual de `HttpClient`.
  - *Desventaja:* Agrega dependencias de terceros y metaprogramación que pueden presentar incompatibilidades con AOT y trimming en ciertas plataformas móviles de .NET MAUI. Se prefirió `HttpClient` nativo con `System.Text.Json`.
- **Lanzar excepciones tradicionales (`HttpRequestException`) hacia la UI:**
  - *Rechazada:* Obligaría a que cada ViewModel implemente múltiples bloques `catch`, degradando el rendimiento en .NET MAUI y complicando la respuesta ante cortes de red o franjas ocupadas.

### Consecuencias

- **Ventajas:**
  - Código desacoplado, testeable y tolerante a fallos de conectividad.
  - Experiencia de usuario consistente gracias a mensajes de error unificados y detección del estado offline.
  - Facilidad de mantenimiento: el contrato API está centralizado en DTOs.
- **Costos y Mitigación:**
  - Requiere mantener los DTOs sincronizados entre la solución cliente y la API (mitigado mediante `api-contract.md` y pruebas de integración).

---

## ADR-0006: Autenticación y manejo de la sesión

- **Estado:** Aceptado
- **Fecha:** 2026-10-28
- **Responsable(s):** M5 (Autenticación, roles y sesión)

### Contexto

La aplicación *BarberTurno* necesita identificar al usuario que la utiliza y su rol (Cliente o Barbero). La sesión debe persistir al cerrar y volver a abrir la app, inclusive sin conexión a internet. El registro permite elegir el rol y la credencial (token) debe almacenarse de forma segura en Android mediante `SecureStorage`.

### Decisión

1. **Login y registro contra la API**
   -     - El servidor devuelve LoginResponseDto con el JWT firmado (Token, que lleva el rol dentro), ExpiraEnUtc, Rol, UsuarioId y Nombre..
2. **Persistencia de la sesión**
   - `SecureStorage` guarda **únicamente**:
     - `auth_token` (string)
     - `auth_expira` (ISO‑8601, UTC) – provisto por el servidor
     - `auth_sesion` (JSON con `UsuarioId`, `Nombre`, `Rol`).
   - **Nunca** se almacena la contraseña.
3. **Restaurar sesión**
   - `LoginViewModel.InicializarAsync()` (llamado desde `OnAppearing` de `LoginPage`) invoca `AuthService.RestaurarSesionAsync()`, que lee token, expiración y datos de sesión de `ISessionStorage`.
   - Si la expiración está a menos de 1 minuto del momento actual, se considera expirada y se solicita login. No se consulta al servidor.
4. **Logout**
   - `ISessionStorage.Clear()` elimina únicamente las claves creadas (`Remove`), dejando intacto cualquier otro dato del Keystore.
   - `SesionActual` se establece a `null`.
5. **Registro**
   - El usuario elige su rol mediante dos `RadioButton` enlazados a `EsCliente`/`EsBarbero`.
   - `AuthService.RegistrarAsync` envía los datos al servidor; la respuesta incluye el JWT con el **rol asignado por el servidor**.
   - La app navega según `SesionActual.Rol` (no según la elección del usuario).
6. **Navegación basada en rol**
   - En la app, el rol solo decide qué interfaz se muestra (`//cliente` o `//barbero`). La seguridad real la aplica el servidor: el rol viaja dentro del JWT firmado y el servidor lo valida en cada petición; si alguien manipula la app para ver pantallas de otro rol, el servidor responderá 401/403 y no entregará datos.
7. **Comunicación de resultados**
   - `ResultadoLogin` y `ResultadoRegistro` son enums que permiten traducir cada caso a mensajes claros en la UI.
8. **Validaciones en ViewModel**
   - Se valida formato de campos (todos obligatorios, nombre ≥3, teléfono 10 dígitos, usuario sin espacios ≥3, contraseña ≥6, coincidencia de contraseñas). Esto mejora la experiencia de usuario; la seguridad se mantiene porque el servidor también valida.
   - La contraseña se limpia de memoria (`Password = ""; ConfirmarPassword = "";`) en el bloque `finally` de cada intento.
9. **Inyección de dependencias**
   - `ISessionStorage` y `IAuthService` se registran como **singleton** porque representan un único punto de gestión de sesión para toda la aplicación.

### Alternativas consideradas

- **Preferences (sin cifrar)** – descartada porque no protege el token.
- **SQLite (archivo sin cifrar)** – descartada por complejidad y falta de cifrado por defecto.
- **Guardar la contraseña** para auto‑login – descartada por riesgo máximo de exposición.
- **No persistir sesión** (pedir login siempre) – descartada por mala experiencia, sobre todo offline.
- **Validar expiración con petición al servidor** al abrir – descartada porque no funciona sin internet.
- **Permitir registro solo de clientes** – descartada por decisión del equipo de permitir ambos roles.

### Consecuencias

#### Ventajas
- La credencial está protegida en Android Keystore y nunca se guarda la contraseña.
- La sesión funciona sin internet gracias a la restauración offline.
- La interfaz cambia según el rol y la lógica de negocio es simple y testeable.
- Rutas absolutas evitan bucles de navegación y el botón Android «atrás» no regresa al login.

#### Costos y riesgos
- **Robo del token**: permite acceso hasta su expiración. Mitigado con expiración razonable definida por el servidor y siempre uso de HTTPS.
- **Fallo de SecureStorage al reinstalar**: se captura la excepción y se limpia la sesión, forzando login. Además se pidió a M1 android:allowBackup="false" en AndroidManifest para evitar restaurar datos cifrados con llaves que ya no existen.
- **Cualquier usuario puede registrarse como Barbero**: el backend valida los roles permitidos; mejora futura: código de invitación o aprobación de administrador.
- **Revocación de token no implementada**: pendiente solicitar al módulo de red la gestión de respuestas 401/403 para redirigir al login.
- **Dependencias externas**: M1 debe registrar `INavigationService`, la ruta `registro`, los `ViewModel`/`Page` en DI, y proveer `IsBusy`/`MensajeError` en `BaseViewModel`. También se necesita el módulo DTO (`RegistroRequestDto`) y el módulo de red (método `RegistrarAsync`, manejo 401/SinConexion, adjuntar token). M1 debe quitar del BarberTurno.csproj los paquetes de servidor (JwtBearer, EntityFrameworkCore), que impiden compilar para Android.

### Pendientes
- Implementar refresh‑token (mejora futura).
- Gestión del 401 con el módulo de red (redirección al login).
- Código de invitación o aprobación para registro de Barberos.
- Descomentar `RegistrarAsync` en `AuthService` cuando el módulo de red lo entregue.

---

## ADR-0007: Inyección de dependencias y ciclos de vida

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M01 (Eliana)

### Contexto
Con MVVM y capas, cada clase necesita colaboradores (repositorios, API, sesión, navegación). Había que decidir cómo se obtienen, cuánto viven y qué comparte estado. Un error de ciclo de vida típico: dos instancias de la sesión (el login guarda el usuario en una y la pantalla de agenda lee otra vacía).

### Decisión
1. Se usa el contenedor de **`Microsoft.Extensions.DependencyInjection`** de MAUI. `MauiProgram` solo encadena un método de registro por capa (`AddDataLayer`, `AddApiLayer`, `AddAuthLayer`, `AddSyncLayer`, `AddNotificationLayer`, `AddBookingLayer`, `AddPresentationLayer`).
2. **Singleton** para lo que tiene estado compartido o un recurso costoso: `IAppDatabase` y repositorios (una conexión), `HttpClient`/`IBarberTurnoApi` (con el token), `IAuthService` (guarda `SesionActual`), `ISessionStorage`, `INetworkService`, `ISyncService`, `INavigationService`, `IDialogService`, `IReminderService`.
3. **Transient** para lo que no debe conservar estado entre usos: todos los ViewModels y páginas, `IBookingService` e `IAvailabilityService`. Así cada pantalla nace limpia.
4. Las páginas y los ViewModels reciben todo por **constructor**; no hay localizadores estáticos. Shell crea las páginas mediante `ContentTemplate`, lo que permite inyectarlas.
5. Regla: un singleton **no** depende de un transient. Para casos opcionales (recordatorios en `BookingService`/`SyncService`) se usan parámetros con valor por defecto, que el contenedor rellena si existen.

### Alternativas consideradas
- **Localizador de servicios / estáticos:** cómodo, pero oculta dependencias y no se puede sustituir en pruebas.
- **Crear las dependencias a mano (`new`):** sin magia, pero acopla las capas y duplica instancias de la conexión y de la sesión.
- **Todo singleton:** evita instanciar, pero los ViewModels conservarían estado de la pantalla anterior (listas, errores) y habría que limpiarlos a mano.
- **Ámbitos (*scoped*) por página:** MAUI no crea un ámbito por página; sería un mecanismo extra sin beneficio claro aquí.

### Consecuencias
- **Ventajas:** dependencias visibles en el constructor, pruebas con Moq sin tocar la aplicación, una sola sesión y una sola conexión.
- **Costos:** los singletons con estado (`SesionActual`) deben **limpiarse al cerrar sesión** (`LogoutAsync`); un olvido filtraría datos de un usuario al siguiente. El encabezado `Authorization` del `HttpClient` singleton es mutable.
- **Riesgos y mitigación:** una dependencia circular rompe el arranque; se evitó haciendo que `SyncService` y `BookingService` dependan de `IReminderService` y no al revés.

---

## ADR-0008: Cómo se evita que dos citas queden a la misma hora

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M04 (Jeifferson) y M08

### Contexto
Es el reto central de la práctica: dos citas no pueden coincidir para el mismo barbero. Parece trivial, pero hay tres escenarios difíciles: (a) dos clientes reservan la misma hora casi a la vez; (b) un teléfono sin conexión reserva una hora que otro ya tomó; (c) un error de programación intenta guardar dos citas iguales. Comprobar la disponibilidad en el teléfono antes de reservar **no es suficiente**: esa comprobación es una foto vieja y no ve lo que ocurre en otros dispositivos. La garantía debe estar donde se ve todo y donde la base de datos pueda hacerla cumplir.

### Decisión
Defensa en profundidad; **la autoridad es el servidor** (ADR-0004) y la última palabra la tiene una restricción de base de datos:

1. **Ofrecer solo horas libres (comodidad, no garantía).** `AvailabilityService` muestra únicamente franjas libres según la duración del servicio.
2. **Comprobación en el servidor, dentro de una transacción.** `POST /api/citas` rechaza con `409` si existe una cita activa que cumpla `existente.Inicio < nuevo.Fin && existente.Fin > nuevo.Inicio`.
3. **Restricción única que cierra la carrera.** Cada cita reserva en `CitaBloques` sus bloques de 5 min; la tabla tiene un **índice único `(BarberoId, BloqueInicioUtc)`**. Los bloques se insertan en la **misma transacción** que la cita. Si dos peticiones pasan a la vez la comprobación del paso 2, la segunda choca con el índice, la base de datos lanza una violación de unicidad y la API la traduce a `409`. Cancelar una cita **libera** sus bloques.
4. **Idempotencia.** El cliente genera el `Id` (Guid) de la cita; repetir el envío (reintento tras perder la red) devuelve la misma cita en vez de duplicarla.
5. **En el teléfono.** Índice único parcial `UX_Cita_BarberoId_Inicio_Activa` sobre `(BarberoId, Inicio)` para citas activas (rechazadas y canceladas no bloquean), y `ExisteSolapamientoAsync` para los solapes parciales. Un choque se traduce a `SlotOcupadoException`.
6. **Reserva sin conexión.** Queda `Pendiente`; al sincronizar, un `409` la pasa a `Rechazada` y se avisa al cliente (ver ADR-0004).
7. **Datos previos.** `docs/db/003_citabloques_relleno.sql` crea los bloques de las citas anteriores al cambio.

### Alternativas consideradas
- **Solo comprobar en la aplicación:** rechazado; no cubre la concurrencia ni el modo sin conexión.
- **Solo "consultar y luego insertar" en el servidor:** es lo que existía primero. Con aislamiento *Read Committed*, dos transacciones pueden pasar la consulta y ambas insertar; el hueco es pequeño pero real.
- **Aislamiento `SERIALIZABLE` con reintentos, o bloqueos `SELECT … FOR UPDATE`:** también correctos, pero más costosos, dependen del motor y hay que gestionar reintentos y bloqueos muertos.
- **Restricción de exclusión de PostgreSQL (`EXCLUDE USING gist` sobre rangos de tiempo):** la solución más elegante y exacta; se descartó por requerir la extensión `btree_gist`, mapeo especial en EF Core y no funcionar en SQLite, que usamos en pruebas locales. Es la mejora natural si se migra a migraciones formales.

### Consecuencias
- **Ventajas:** ni dos peticiones simultáneas ni un error de programación pueden dejar dos citas activas solapadas en la misma hora: la base de datos lo impide. El conflicto llega al usuario como un mensaje claro.
- **Costos:** una tabla auxiliar y varias filas por cita; hay que mantenerla coherente al cancelar (hecho) y rellenarla para datos antiguos (script 003). Los bloques se redondean hacia abajo a 5 min, por lo que una hora no alineada puede producir un falso conflicto de pocos minutos, nunca un solape sin detectar.
- **Riesgos y mitigación:** el índice local solo detecta el mismo inicio; los solapes parciales dependen de `ExisteSolapamientoAsync` y, sobre todo, del servidor. No hay aún una prueba automatizada de concurrencia contra la API (las pruebas unitarias cubren el solape local y los mapeadores); es la prueba pendiente más importante antes de la defensa: lanzar dos `POST` simultáneos y verificar un solo `201` y un `409`.

---

## ADR-0009: Recordatorios con notificaciones locales

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M06

### Contexto
El cliente debe recibir un aviso en el teléfono antes de su cita, con el permiso correspondiente. Hay que decidir cómo se programa, qué pasa si la cita se cancela, se rechaza o cambia, y qué ocurre con el permiso en Android 13+ (`POST_NOTIFICATIONS`).

### Decisión
1. Se usa **`Plugin.LocalNotification` 14.1** (`UseLocalNotification()` en `MauiProgram`), con notificaciones **locales programadas** (no hay servidor de notificaciones).
2. `ReminderService` (implementa `IReminderService`, singleton) avisa **60 minutos antes** de la cita; si ya falta menos, avisa en unos segundos; si la cita ya empezó, no avisa.
3. El **id** de la notificación se deriva del Guid de la cita (`IdNotificacion`), de modo que se puede cancelar o reemplazar sin guardar nada más.
4. **Permiso:** `ProgramarAsync` consulta y, si falta, **solicita** el permiso de notificaciones en el momento de reservar (contexto claro para el usuario). Sin permiso la reserva sigue siendo válida; solo no se programa el aviso.
5. **Cuándo se programa:** al confirmar la reserva, al guardar una reserva pendiente sin conexión y cuando una pendiente se confirma al sincronizar. **Cuándo se cancela:** al cancelar la cita, al ser rechazada (`409` al sincronizar) y, en la reconciliación, cuando el servidor la marca cancelada.
6. **Reconciliación:** tras sincronizar las citas, `ReconciliarAsync` recorre las citas del cliente y programa las vigentes y cancela las que ya no corresponden, así los recordatorios se corrigen aunque la cita cambie desde otro dispositivo.
7. Cualquier fallo del plugin se registra y se ignora: un recordatorio nunca debe impedir una reserva.

### Alternativas consideradas
- **Notificaciones push (FCM/APNs):** funcionan con la app cerrada y desde el servidor, pero exigen cuenta de Firebase, un servicio emisor y gestión de tokens; desproporcionado y no pedido.
- **`AlarmManager`/`UNUserNotificationCenter` nativos directos:** control total, pero código distinto por plataforma; el plugin los envuelve.
- **Tareas en segundo plano periódicas:** Android las limita (ahorro de batería) y no son puntuales.

### Consecuencias
- **Ventajas:** funciona sin conexión, sin infraestructura extra; el aviso sigue la vida de la cita (cancelar/rechazar lo elimina).
- **Costos:** en Android 12+ las alarmas exactas requieren permiso especial; si el sistema lo niega, el aviso puede llegar con algo de retraso. Si el usuario borra los datos de la app o cambia de teléfono, los avisos locales se pierden hasta la siguiente sincronización (la reconciliación los recrea).
- **Riesgos y mitigación:** en Windows sin empaquetar (MSIX) las notificaciones pueden no mostrarse; el entorno objetivo es el emulador/teléfono Android. Aún no existe la pantalla de **reprogramar** cita; `ReprogramarAsync` ya cancela y reprograma el aviso para cuando se implemente.

---

## ADR-0010: Manejo de estados de interfaz y errores

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M09 (Albert Frank)

### Contexto
Toda pantalla que carga datos pasa por cuatro situaciones: cargando, sin datos, con error y con datos. Sin un criterio común, cada pantalla inventa sus banderas (`IsLoading`, `HasError`…), los errores de red llegan como excepciones y el usuario ve pantallas en blanco o aplicaciones que se cierran. La reserva, además, debe validarse antes de enviarse.

### Decisión
1. **Un enumerado único `ViewState`** (`Cargando`, `Vacio`, `Error`, `ConDatos`) en `BaseViewModel.State`, con `MensajeError` para el texto.
2. Dos controles de presentación reutilizables, sin lógica de negocio:
   - **`StateView`**: sustituye el contenido de la pantalla según el estado (listas de barberos, servicios, mis citas, hora, confirmar reserva, perfil), con botón **Reintentar** enlazado a un comando.
   - **`EstadoBanner`**: indicador compacto de carga/error para pantallas con cabecera y contenido propios (Mi agenda, Calendario, Inicio, Clientes, Mis servicios, Detalle de cita); vacío y con datos los resuelve la pantalla.
3. **Errores de red como valores, no como excepciones:** la capa de red devuelve `ApiResult<T>` (`Exito`, `CodigoHttp`, `SinConexion`, `Error`) y los ViewModels lo traducen a un estado y un mensaje en español.
4. **Sin conexión:** se muestra un aviso ("mostrando datos guardados") y se sigue leyendo de SQLite.
5. **Validación de la reserva** en `ConfirmarReservaViewModel.Validar()` antes de enviar (barbero, servicio y hora elegidos, y hora todavía en el futuro), seguida de una **revalidación** de que la hora sigue libre; un `409` del servidor se muestra como "hora ocupada" y no como error genérico.
6. Los ViewModels capturan las excepciones de sus cargas (`try/catch`) y pasan a `Error`; la aplicación nunca se cierra por un fallo de red.

### Alternativas consideradas
- **Banderas booleanas por pantalla:** simples al inicio, pero combinaciones inválidas (cargando y error a la vez) y duplicación en cada ViewModel.
- **Propagar excepciones hasta la interfaz con `try/catch` en cada vista:** dispersa el manejo de errores y complica los mensajes.
- **Ventanas emergentes (`DisplayAlert`) para todo:** interrumpen al usuario y no permiten reintentar desde la propia pantalla.

### Consecuencias
- **Ventajas:** comportamiento coherente en toda la app, ViewModels más pequeños y fáciles de probar (se comprueba `State`), reintento en un toque.
- **Costos:** dos controles con propósitos parecidos; el equipo debe elegir bien (¿la pantalla se reemplaza o tiene contenido propio?).
- **Riesgos y mitigación:** una carga que olvide poner `State = ConDatos` deja el indicador girando; las pruebas de ViewModel verifican el estado final de cada carga.

---

## ADR-0011: Tema visual claro y oscuro

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M10 (Equipo)

### Contexto
La práctica exige soporte de tema claro y oscuro y una interfaz cuidada. La marca de la barbería pide una paleta sobria (carbón, crema y dorado). Había que decidir cómo se definen los colores una sola vez, cómo cambia el tema y si la elección del usuario se recuerda.

### Decisión
1. Los colores son **tokens con nombre** en `Resources/Styles` (`M10Light*` y `M10Dark*` para página, superficie, texto, borde, tarjeta y dorado). Las vistas no escriben colores sueltos salvo contadas excepciones.
2. Cada token se usa con **`AppThemeBinding`** (`Light=…, Dark=…`), así la interfaz reacciona al instante cuando cambia el tema.
3. Estilos reutilizables (`BtCard`, `BtDarkCard`, `BtField`, `BtGoldButton`, `BtTitle`, …) concentran tipografía (Open Sans), radios y espacios.
4. El tema lo elige el usuario con un interruptor en **Perfil**; se guarda en `Preferences` (`tema_oscuro`) y `App` lo aplica al iniciar (`UserAppTheme`).
5. Los íconos son SVG; los que no se leen sobre fondo oscuro tienen variante (`icon_bell` / `icon_bell_dark`) elegida con `AppThemeBinding`. La barra de estado de Android toma el color de fondo de la app.

### Alternativas consideradas
- **Seguir solo el tema del sistema:** menos código, pero el usuario no puede elegir; la práctica pide soporte explícito de ambos temas.
- **Intercambiar `ResourceDictionary` en tiempo de ejecución con `DynamicResource`:** flexible, pero reemplazar diccionarios completos es más frágil y propenso a olvidos que `AppThemeBinding` por propiedad.
- **Librería de componentes Material:** acelera, pero impone su estética y añade una dependencia grande.

### Consecuencias
- **Ventajas:** los colores se cambian en un solo lugar, ambos temas siempre coherentes, preferencia recordada entre sesiones.
- **Costos:** `AppThemeBinding` es verboso en XAML; cada color nuevo exige definir la pareja claro/oscuro.
- **Riesgos y mitigación:** quedan algunos colores fijos en vistas antiguas y los SVG monocromos necesitan su variante oscura a mano. Se revisan en cada pantalla nueva al probar en ambos temas.

---

## ADR-0012: Configuración y secretos; hosting de la API

- **Estado:** Aceptado
- **Fecha:** 2026-10-27
- **Responsable(s):** Módulo 6 (Cliente de API y Hosting)

### Contexto
El backend de *BarberTurno* (construido con ASP.NET Core) debe estar alojado en un entorno accesible por la aplicación móvil .NET MAUI tanto durante la fase de desarrollo local (emuladores y dispositivos físicos) como en el despliegue final para la defensa y uso en producción.

Requerimientos clave:
1. Ninguna credencial de base de datos, clave de cifrado JWT ni URLs absolutas deben quedar fijas (*hardcoded*) en el código fuente ni expuestas en repositorios públicos.
2. La API debe ser accesible por clientes móviles que operan desde distintos orígenes y redes (requiriendo soporte de CORS y políticas de seguridad adecuadas).
3. Comunicación obligatoria por canal seguro cifrado (**HTTPS**) en la versión final de entrega.
4. Soporte sencillo para desarrollo local mediante emulador de Android (red `10.0.2.2`) y clientes de escritorio (`localhost`).

### Decisión

1. **Gestión de Configuración y Secretos por Capas:**
   - En el servidor (`BarberTurno.Api`), `appsettings.json` contiene **solo valores no secretos** (emisor, audiencia y duración del JWT). Ni la cadena de conexión ni la clave de firma están en el repositorio.
   - La cadena de conexión (`ConnectionStrings:DefaultConnection`) y la clave de firma (`Jwt:Key`, mínimo 32 caracteres) se entregan con **`dotnet user-secrets`** en desarrollo y con **variables de entorno** (`ConnectionStrings__DefaultConnection`, `Jwt__Key`) en el servidor. Si faltan, la API **se detiene al iniciar** con un mensaje explicativo (*fail fast*) en vez de usar un valor por defecto. `appsettings.Development.example.json` solo trae marcadores de posición.
   - En la aplicación móvil (.NET MAUI), la dirección base de la API se abstrae en `AppConstants.ApiBaseUrl`: el **emulador** de Android usa `http://10.0.2.2:5000/api/` (la API local de la PC) y un **teléfono real** usa la URL HTTPS pública de producción.

2. **CORS (Cross-Origin Resource Sharing):**
   - Se configuró una política abierta `"AllowAll"` en el pipeline de ASP.NET Core (`Program.cs`) que autoriza cualquier encabezado, origen y método HTTP, garantizando que clientes Web o híbridos puedan consumir la API sin bloqueos de navegador.

3. **Estrategia de Hosting de la API:**
   - La API se despliega en **Render** como servicio web con **Docker** (`src/BarberTurno.Api/Dockerfile`, *Root Directory* `src/BarberTurno.Api`), conectado al repositorio de GitHub: cada `push` a la rama configurada vuelve a desplegar. Render termina el HTTPS (certificado gratuito); la API escucha en el puerto de la variable `PORT` y no fuerza la redirección HTTPS por sí misma.
   - La base de datos es **PostgreSQL en Supabase** (cadena del *pooler*). Los cambios de esquema se aplican con los scripts numerados de `docs/db/` (ver README), ya que `EnsureCreated()` no modifica tablas existentes.
   - El tráfico sin cifrar (`usesCleartextTraffic`) sigue permitido en el manifiesto para el desarrollo con emulador; **queda pendiente desactivarlo en la versión final**, ya que la app de producción solo habla con la URL HTTPS.

4. **Documentación Interactiva con Swagger/OpenAPI:**
   - Se integró `Swashbuckle.AspNetCore` configurado para ejecutarse en la raíz (`/`), permitiendo al resto del equipo probar endpoints y probar la autenticación Bearer Token interactivamente sin necesidad de herramientas externas como Postman.

### Alternativas consideradas

- **Quemar la clave secreta y la URL directamente en constantes de C#:**
  - *Rechazada terminantemente:* Viola las buenas prácticas de seguridad de software, impide cambiar de entorno sin recompilar y expondría secretos si el repositorio se hace público.
- **Base de datos remota pesada (SQL Server o PostgreSQL remoto durante desarrollo inicial):**
  - *Descartada en fase inicial:* Se optó por **SQLite** con Entity Framework Core en el servidor, ya que es liviano, portátil, no requiere instalar servicios de base de datos externos para ejecutar los tests de integración y puede migrarse a PostgreSQL/SQL Server fácilmente cambiando el proveedor en `Program.cs`.

### Consecuencias

- **Ventajas:**
  - Despliegue ágil, portátil y seguro.
  - Integración sin fricción para los desarrolladores del equipo en cualquier plataforma (Windows, Android, Mac).
  - Cumplimiento de la regla de oro: cero secretos sensibles en el repositorio de Git.
- **Riesgos y Mitigación:**
  - El emulador de Android requiere `10.0.2.2` en lugar de `localhost`. Mitigado mediante la directiva condicional `#if ANDROID` en `AppConstants.cs`.
- **Costo del hosting gratuito:** el servicio de Render se suspende tras un rato sin uso y la primera petición tarda hasta un minuto en despertarlo; la app lo trata como un tiempo de espera normal (15 s de *timeout* y reintento al volver a abrir).
