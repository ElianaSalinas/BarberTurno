# BarberTurno

Aplicación móvil de reservas para una barbería, desarrollada con **.NET MAUI**.
Práctica integradora de **ISW-126 · Desarrollo de Aplicaciones Móviles I**, Universidad Central del Este.

> Este archivo es el "archivo de instrucciones" exigido en los entregables: descripción de la aplicación,
> modo de compilar y ejecutar, usuarios de prueba y resumen de la arquitectura.

**Equipo:** TODO (nombres de los 10 integrantes y su código M01-M10, ver sección 11)
**Profesor:** TODO · **Periodo:** TODO

---

## 1. Descripción

BarberTurno permite reservar turno en una barbería desde el teléfono. Tiene dos tipos de usuario, cada uno con su propia
experiencia dentro de la misma aplicación.

**Cliente**
- Inicia sesión y ve la lista de barberos con los servicios que ofrecen.
- Escoge barbero, servicio y una hora libre, y confirma la reserva.
- Consulta sus próximas citas y las anteriores.
- Recibe un aviso en el teléfono antes de la cita.

**Barbero**
- Inicia sesión y ve su agenda del día, ordenada por hora.
- Consulta el historial de visitas del cliente de cada cita.
- (Según alcance) marca una cita como atendida o registra una ausencia.

**Reto central:** dos citas no pueden quedar a la misma hora con el mismo barbero, ni con reservas casi simultáneas ni con
reservas hechas sin conexión. Ver sección 6 y `docs/adr/0008-*`.

## 2. Requisitos obligatorios y estado

Marcar al completar cada uno (la lista de verificación de la sección 12 se revisa antes de la defensa).

| Requisito | Cómo se cumple en este proyecto | Estado |
|---|---|---|
| Autenticación y roles | Login de cliente y barbero, interfaz por rol, credencial en SecureStorage, cierre de sesión | [ ] |
| Persistencia local | Cliente, Barbero, Servicio y Cita en SQLite mediante repositorios | [ ] |
| Consumo de API | `HttpClient` + DTOs + traductor hacia las entidades locales | [ ] |
| Uso sin conexión | Consulta de datos sin señal, reserva offline `PendienteConfirmacion`, detección de red | [ ] |
| Reserva de turno | Barbero + servicio + hora libre según duración del servicio | [ ] |
| Agenda e historial | Agenda del día del barbero e historial del cliente por cita | [ ] |
| Sin citas solapadas | Restricción en la base del servidor + 409 + idempotencia + índice local de apoyo | [ ] |
| Aviso de la cita | Notificación local con permiso, cancelada o reprogramada si cambia la cita | [ ] |
| Estados e interfaz | Carga, vacío, error y con datos; validación de la reserva | [ ] |
| Arquitectura e interfaz | MVVM + DI, Shell por perfil, tema claro y oscuro, interfaz cuidada | [ ] |

Opcionales que pretendemos cubrir: TODO (cancelar/reprogramar, foto de perfil o mapa, calificación, sugerir próxima hora libre,
pruebas unitarias de un ViewModel, carga por páginas / pull-to-refresh).

## 3. Tecnología

| Área | Elección |
|---|---|
| Plataforma | .NET MAUI (versión del laboratorio), C# y XAML. Destinos: **Android (emulador) y Windows** |
| Presentación | MVVM con **CommunityToolkit.Mvvm**; navegación con **Shell** por perfil |
| Datos locales | **SQLite** (`sqlite-net-pcl`) mediante patrón repositorio |
| Servidor | **API ASP.NET Core** (`src/BarberTurno.Api`) + **PostgreSQL en Supabase** (fuente de verdad) |
| Red | `HttpClient`, DTOs y traductor (`Mappers/`) |
| Seguridad | **SecureStorage** para el token; JWT; sin secretos en el repositorio |
| Notificaciones | Notificaciones locales (Plugin.LocalNotification o equivalente) |
| Inyección de dependencias | `Microsoft.Extensions.DependencyInjection` (integrada en MAUI) |

## 4. Resumen de la arquitectura

```
   Android (cliente)        Windows (barbero)
          |                        |
          |   Views (XAML)  ->  ViewModels  ->  Services  ->  Repositories  ->  SQLite (local)
          |                                        |
          |                                        +--> IBarberTurnoApi (HttpClient + DTOs + Mappers)
          |                                                   |   HTTPS + JWT
          +---------------------------+-----------------------+
                                      v
                          API ASP.NET Core (autoridad)
                                      |  Npgsql (Session Pooler)
                                      v
                          PostgreSQL en Supabase
```

**Capas de la app** (`src/BarberTurno`)

| Carpeta | Contenido | Regla |
|---|---|---|
| `Models/` | Entidades del dominio y enums | Sin dependencias de UI |
| `Data/` | `AppDatabase`, repositorios, excepciones | Solo aquí se toca SQLite |
| `Dtos/` y `Mappers/` | Forma del JSON de la API y su traducción | Si la API renombra un campo, solo cambian estas dos carpetas |
| `Services/` | Auth, Api, Network, Sync, Availability, Booking, Notifications, Navigation, Dialogs | Aquí vive la lógica de negocio |
| `ViewModels/` | Estado y comandos de cada pantalla | Delegan en los servicios; sin acceso directo a datos |
| `Views/` y `Controls/` | Páginas XAML y `StateView` | **Sin lógica de negocio en el code-behind** |
| `Helpers/` | `Routes`, `ViewState`, `AppConstants`, converters | Constantes, sin secretos |
| `Resources/Styles/` | Colores, estilos y tema claro/oscuro | |

Cada capa registra sus servicios en su propio `*ServiceRegistration.cs`; `MauiProgram.cs` solo los encadena.

**Navegación (Shell por perfil):** `login` -> `//cliente` (TabBar: Barberos, Mis citas, Perfil) o `//barbero`
(TabBar: Agenda, Perfil). Las páginas apiladas (Servicios, Selección de hora, Confirmar reserva, Historial, Detalle)
se registran en `AppShell.xaml.cs`; los ViewModels navegan con `INavigationService` y las constantes de `Routes`.

## 5. Modelo de datos

```
Cliente(Id, Nombre, Telefono, FotoRuta?)
Barbero(Id, Nombre, FotoUrl?)
Servicio(Id, Nombre, DuracionMin, Precio)
BarberoServicio(BarberoId, ServicioId)
Cita(Id[Guid], ClienteId, BarberoId, ServicioId, Inicio, Fin, Estado, MotivoRechazo?)
```

- Cambios respecto al modelo sugerido: se añade `BarberoServicio` (qué servicios ofrece cada barbero), `FotoUrl` en Barbero y
  `MotivoRechazo` en Cita. El `Id` de la cita es un `Guid` generado en el dispositivo (sirve para trabajar sin conexión y como
  clave de idempotencia). Justificación completa en los ADR 0003 y 0008.
- **Estados de la cita:** `PendienteConfirmacion`, `Confirmada`, `Rechazada`, `Cancelada` (y opcionales `Atendida`, `Ausente`).
- `Fin` = `Inicio` + duración del servicio. Las fechas viajan en UTC y se muestran en hora local.
- **Disponibilidad:** franjas según la duración del servicio dentro del horario del barbero, descartando las que se solapan con
  citas activas. El servidor calcula (autoridad); el teléfono hace un cálculo aproximado offline. Ver ADR 0004 y 0008.

## 6. Cómo se evita que dos citas queden a la misma hora

1. **El servidor (PostgreSQL) es la autoridad.** Comprobar disponibilidad en el teléfono no basta: los datos pueden estar
   desactualizados y otro usuario puede reservar entre la consulta y la confirmación.
2. **La base garantiza la regla**, no el código: restricción única sobre barbero + inicio **solo para citas activas**
   (índice filtrado) o, mejor, una restricción de exclusión por rango de tiempo / tabla de bloques ocupados, que cubre
   servicios de distinta duración.
3. `POST /citas` inserta de forma atómica; si la base rechaza el insert, la API responde **409 Conflict**.
4. **Idempotencia:** el `Id` de la cita lo genera el dispositivo; un reintento tras perder la conexión no crea una segunda cita.
5. **Sin conexión:** la reserva queda `PendienteConfirmacion`; al sincronizar pasa a `Confirmada` o `Rechazada` (409) y se avisa
   al cliente. Un índice único local evita solapes dentro del propio teléfono.

Detalle y alternativas descartadas: `docs/adr/0008-*`.

## 7. Seguridad y configuración

- La **credencial** (token) se guarda en `SecureStorage`, nunca en SQLite ni en `Preferences`; no se guarda la contraseña.
- Las contraseñas se almacenan con hash en el servidor.
- **Cero secretos en el repositorio:** la cadena de conexión de Supabase, la clave JWT y la clave `service_role` viven como
  variables de entorno del servidor (o `dotnet user-secrets` en desarrollo). El `.gitignore` ya excluye `.env*`,
  `appsettings.Development.json`, `secrets.json` y similares.
- Las apps **no** se conectan a la base de datos: solo hablan con la API.
- En Supabase: sin la Data API expuesta (o con RLS activado en todas las tablas) y conexión mediante el **Session Pooler**.

## 8. Compilar y ejecutar

### 8.1 Requisitos previos
- Visual Studio 2022 o superior con la carga de trabajo **.NET Multi-platform App UI**, y el SDK de .NET de la versión del laboratorio.
- Emulador de Android configurado (o ejecutar el destino Windows).
- Cuenta de Supabase (plan Free) y un servidor para alojar la API (p. ej. Render).

### 8.2 Preparar el proyecto MAUI (una sola vez, lo hacen los líderes)
1. Crear la plantilla en una carpeta temporal: `dotnet new maui -n BarberTurno -o tmp/BarberTurno`.
2. Copiar a `src/BarberTurno/` los archivos de plataforma generados (`BarberTurno.csproj`, `Platforms/`, `Properties/`,
   `Resources/` con Fonts, Images, Raw, Splash, AppIcon y `Styles/Colors.xaml` + `Styles.xaml`) **sin sobrescribir** los
   archivos de este repositorio (`App.xaml(.cs)`, `AppShell.xaml(.cs)`, `MauiProgram.cs`, `Resources/Styles/AppTheme.xaml`).
   No copiar `MainPage.xaml(.cs)`.
3. Instalar paquetes (dentro de `src/BarberTurno`):
   ```
   dotnet add package CommunityToolkit.Mvvm
   dotnet add package sqlite-net-pcl
   dotnet add package SQLitePCLRaw.bundle_green
   dotnet add package Plugin.LocalNotification
   dotnet add package CommunityToolkit.Maui        # opcional
   ```
4. **Quitar iOS y Mac Catalyst** (no se compilan en Windows) en `BarberTurno.csproj`:
   `<TargetFrameworks>net9.0-android;net9.0-windows10.0.19041.0</TargetFrameworks>` (ajustar `net9.0` a la versión del laboratorio).
5. Comprobar que `RootNamespace` sea `BarberTurno`.

### 8.3 Soluciones separadas (importante)
La app y la API **no** comparten solución. Si lo hicieran, al elegir un destino de la app Visual Studio intentaría compilar la
API para iOS/Mac Catalyst y fallaría con `NETSDK1082 ... runtime pack for Microsoft.AspNetCore.App ... iossimulator-x64`.
```
dotnet new sln -n BarberTurno
dotnet sln add src/BarberTurno/BarberTurno.csproj

cd src/BarberTurno.Api
dotnet new sln -n BarberTurno.Api
dotnet sln add BarberTurno.Api.csproj
```

### 8.4 Base de datos y API (Supabase)
1. Crear el proyecto en Supabase (Postgres por defecto). Recomendado: no conectar GitHub, desactivar **Enable Data API**
   (o activar RLS automático y no exponer tablas nuevas).
2. En el botón **Connect** copiar la cadena del **Session Pooler** (IPv4, puerto 5432, usuario `postgres.<ref>`).
3. Configurar la API **sin escribir secretos en archivos versionados**:
   ```
   cd src/BarberTurno.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:Default" "<cadena del Session Pooler>"
   dotnet user-secrets set "Jwt:Key" "<clave larga aleatoria>"
   ```
   En el servidor (Render u otro) se definen como variables de entorno equivalentes.
4. Crear el esquema y los índices únicos (ver `docs/adr/0008-*`) y cargar datos de prueba (barberos, servicios, usuarios).
5. Los proyectos del plan Free de Supabase **se pausan tras una semana sin actividad**: reactivar el proyecto el día antes de la defensa.

### 8.5 Ejecutar
1. Arrancar la API (local o en el servidor) y anotar su URL base HTTPS.
2. Poner esa URL en la configuración de la app (`AppConstants` / configuración local; no es un secreto, pero no se versionan claves).
   Si la API corre en el PC y la app en el emulador de Android, usar `https://10.0.2.2:<puerto>`.
3. Abrir la solución de la app, elegir **Android Emulator** o **Windows Machine** (nunca iOS/Mac Catalyst) y ejecutar.
4. Para la demostración: cliente en el emulador de Android y barbero en Windows (o dos emuladores), ambos contra la misma API.

## 9. Usuarios de prueba

> TODO(M03): completar antes de la entrega con datos de ejemplo. No usar credenciales reales.

| Rol | Usuario | Contraseña | Notas |
|---|---|---|---|
| Cliente | TODO | TODO | |
| Cliente | TODO | TODO | Para probar dos reservas casi simultáneas |
| Barbero | TODO | TODO | Con citas cargadas para la agenda del día |

## 10. Estructura del repositorio

```
BarberTurno/
├─ README.md
├─ .gitignore
├─ src/
│  ├─ BarberTurno/          App MAUI (Models, Data, Dtos, Mappers, Services, ViewModels, Views, Controls, Helpers)
│  └─ BarberTurno.Api/      API ASP.NET Core (Controllers, Domain, Data, Services, Dtos)
├─ tests/                   Pruebas unitarias (opcional)
└─ docs/
   ├─ adr/                  Decisiones de arquitectura (0001 a 0012)
   ├─ api-contract.md       Contrato de endpoints
   ├─ diseno/               Flujos de usuario y bocetos
   └─ reparto.md            Responsabilidades del equipo
```

**ADR (carpeta `docs/adr/`)**

| # | Decisión | Obligatorio |
|---|---|---|
| 0001 | Patrón de presentación (MVVM) | Sí |
| 0002 | Navegación entre perfiles de cliente y barbero | Sí |
| 0003 | Persistencia local con SQLite | Sí |
| 0004 | Datos y sincronización: fuente de verdad y modo sin conexión | Sí |
| 0005 | Capa de red y traducción de datos | Sí |
| 0006 | Autenticación y manejo de la sesión | Sí |
| 0007 | Inyección de dependencias y ciclos de vida | Sí |
| 0008 | Cómo se evita que dos citas queden a la misma hora | Sí |
| 0009 | Recordatorios con notificaciones locales | Recomendado |
| 0010 | Manejo de estados de interfaz y errores | Recomendado |
| 0011 | Tema visual claro/oscuro | Recomendado |
| 0012 | Configuración y secretos; hosting de la API | Recomendado |

Cada ADR sigue la plantilla `docs/adr/0000-plantilla.md` (contexto, decisión, alternativas reales, consecuencias con sus costos).

## 11. Equipo y responsabilidades

| Id | Integrante | Área | Carpetas principales |
|---|---|---|---|
| M01 | TODO | Arquitectura, Shell, DI, integración, Git | `App*`, `MauiProgram`, `Helpers`, `Services/Navigation`, `Services/Dialogs` |
| M02 | TODO | Modelos y SQLite | `Models/`, `Data/` |
| M03 | TODO | API: base, autenticación, hosting | `BarberTurno.Api` (Program, Auth, Barberos, Servicios, Domain, Data) |
| M04 | TODO | API: disponibilidad y reservas (concurrencia) | `BarberTurno.Api` (Citas, Disponibilidad, Services) |
| M05 | TODO | Red del cliente, DTOs, traductor | `Services/Api`, `Dtos/`, `Mappers/` |
| M06 | TODO | Autenticación, SecureStorage, notificaciones | `Services/Auth`, `Services/Notifications`, Login |
| M07 | TODO | Sin conexión y sincronización | `Services/Network`, `Services/Sync` |
| M08 | TODO | Disponibilidad y reserva (cliente) | `Services/Availability`, `Services/Booking`, SeleccionHora, ConfirmarReserva |
| M09 | TODO | UI del cliente y estados de pantalla | `Views/Cliente`, Perfil, `Controls/StateView`, Converters |
| M10 | TODO | UI del barbero y tema claro/oscuro | `Views/Barbero`, `Resources/Styles` |

Todos deben poder explicar el conjunto de la aplicación en la defensa, no solo su parte.

## 12. Lista de verificación previa a la defensa

- [ ] La aplicación compila y se ejecuta siguiendo este archivo.
- [ ] Inicio de sesión de cliente y barbero con credencial segura y usuarios de prueba documentados.
- [ ] Cliente, Barbero, Servicio y Cita se guardan en SQLite.
- [ ] Sincronización con la API mediante DTOs y traductor, con manejo de errores de red.
- [ ] Funciona sin conexión y detecta el estado de la red.
- [ ] El cliente reserva escogiendo barbero, servicio y una hora libre.
- [ ] El barbero ve su agenda del día y el historial de cada cliente.
- [ ] **No es posible crear dos citas a la misma hora para un mismo barbero** (demostrable).
- [ ] El recordatorio funciona y se pide el permiso de notificaciones.
- [ ] Las pantallas con datos muestran los cuatro estados y la reserva se valida.
- [ ] MVVM + DI + Shell, sin lógica en el code-behind.
- [ ] Tema claro y oscuro e interfaz cuidada.
- [ ] ADR requeridos completos, incluido el de las citas solapadas.
- [ ] Control de versiones con aportes de todos y sin secretos en el repositorio.
- [ ] Material de diseño (flujos y bocetos) en `docs/diseno/`.
- [ ] Presentación preparada y dominio individual del trabajo.

## 13. Normas de trabajo

- **Git:** una rama por tarea (`feature/m05-api-client`), Pull Request revisado por otro integrante, mensajes de commit claros,
  aportes de todos reflejados en el historial.
- **Calidad:** nombres claros, comentarios donde aporten, asincronía correcta, ninguna lógica de negocio en el code-behind.
- **Secretos:** ninguna clave ni credencial dentro del repositorio.
- **Integridad académica:** se puede consultar documentación y herramientas de apoyo, pero cada integrante debe comprender y
  poder defender cada parte del trabajo.
