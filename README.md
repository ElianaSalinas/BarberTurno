# BarberTurno

Aplicación móvil de reservas para una barbería desarrollada con **.NET MAUI**, arquitectura **MVVM**, persistencia local con **SQLite** y sincronización con una **API REST en ASP.NET Core** respaldada por **PostgreSQL (Supabase)**.

> Proyecto correspondiente a la práctica integradora **ISW-126**, Universidad Central del Este (**UCE**).

---

## 👥 Reparto de Responsabilidades y Módulos

| Módulo | Responsable | Área / Funcionalidad | Carpetas / Componentes Clave | ADR Asociados |
| :--- | :--- | :--- | :--- | :--- |
| **M01** | **Eliana** | Arquitectura base, Shell, Inyección de Dependencias (DI), integración general y control de versiones | `App.xaml`, `AppShell`, `MauiProgram`, `Helpers/`, `Services/Navigation`, `Services/Dialogs`, `BaseViewModel` | [0001](docs/adr/0001-patron-de-presentacion.md), [0002](docs/adr/0002-navegacion-entre-perfiles-de-cliente-y-barber.md), [0007](docs/adr/0007-inyeccion-de-dependencias-y-ciclos-de-vida.md) |
| **M02** | **Brailyn** | Modelado de datos de dominio y persistencia local SQLite | `Models/`, `Data/` | [0003](docs/adr/0003-persistencia-local-con-sqlite.md) |
| **M03** | **Equipo** | API: Estructura base, autenticación y despliegue (Hosting) | `src/BarberTurno.Api/` (`Program.cs`, `Auth/`, `Barberos/`, `Servicios/`, `Domain/`, `Data/`) | [0012](docs/adr/0012-configuracion-y-secretos-hosting-de-la-api.md) |
| **M04** | **Jeifferson** | API: Disponibilidad, reservas, concurrencia en servidor y optimización | `src/BarberTurno.Api/Controllers/`, `Data/Repositories/`, `Services/Disponibilidad/` | [0008](docs/adr/0008-como-se-evita-que-dos-citas-queden-a-la-misma.md) |
| **M05** | **Elian** | Capa de red del cliente móvil, DTOs y traductores (Mappers) | `Services/Api/`, `Dtos/`, `Mappers/` | [0005](docs/adr/0005-capa-de-red-y-traduccion-de-datos.md) |
| **M06** | **Equipo** | Autenticación en app, almacenamiento seguro (`SecureStorage`) y notificaciones locales | `Services/Auth/`, `Services/Notifications/`, `Views/Login/` | [0006](docs/adr/0006-autenticacion-y-manejo-de-la-sesion.md), [0009](docs/adr/0009-recordatorios-con-notificaciones-locales.md) |
| **M07** | **Jorge Cabrera** | Modo sin conexión (Offline) y sincronización con servidor | `Services/Network/`, `Services/Sync/` | [0004](docs/adr/0004-datos-y-sincronizacion-fuente-de-verdad-y-mod.md) |
| **M08** | **Equipo** | Flujo de reserva y consulta de disponibilidad (lado cliente) | `Services/Availability/`, `Services/Booking/`, pantallas de selección de hora y confirmación | [0008](docs/adr/0008-como-se-evita-que-dos-citas-queden-a-la-misma.md) |
| **M09** | **Albert Frank** | Interfaz de usuario (UI) del cliente y manejo de estados de pantalla | `Views/Cliente/`, `Views/Perfil/`, `Controls/StateView/`, `Converters/` | [0010](docs/adr/0010-manejo-de-estados-de-interfaz-y-errores.md) |
| **M10** | **Equipo** | Interfaz de usuario (UI) del barbero y soporte tema claro/oscuro | `Views/Barbero/`, `Resources/Styles/` | [0011](docs/adr/0011-tema-visual-claro-oscuro.md) |

---

## 📁 Estructura del Repositorio

```text
BarberTurno/
├── BarberTurno.sln        # Solución principal de .NET
├── src/
│   ├── BarberTurno/       # Aplicación móvil .NET MAUI (Cliente)
│   │   ├── Controls/      # Controles reutilizables (StateView, etc.)
│   │   ├── Converters/    # Conversores para Data Binding XAML
│   │   ├── Data/          # Repositorios y contexto SQLite local
│   │   ├── Dtos/          # Objetos de transferencia de datos
│   │   ├── Helpers/       # Constantes y utilidades (AppConstants)
│   │   ├── Mappers/       # Mapeo entre Modelos y DTOs
│   │   ├── Models/        # Modelos de dominio del cliente
│   │   ├── Platforms/     # Código específico por plataforma (Android, iOS, Windows)
│   │   ├── Resources/     # Recursos estáticos (estilos, fuentes, iconos, splash)
│   │   ├── Services/      # Lógica de negocio, API client, sync, auth
│   │   ├── ViewModels/    # Lógica de presentación (CommunityToolkit.Mvvm)
│   │   └── Views/         # Pantallas XAML (Cliente, Barbero, Auth)
│   └── BarberTurno.Api/   # Backend API REST (ASP.NET Core .NET 9)
│       ├── Controllers/   # Controladores REST protegidos con JWT
│       ├── Data/          # ApiDbContext (PostgreSQL / EF Core 9)
│       ├── Domain/        # Entidades del servidor
│       ├── Dtos/          # DTOs de entrada y salida
│       └── Services/      # Lógica de concurrencia y disponibilidad
├── docs/                  # Documentación técnica, ADRs y contrato de API
│   ├── adr/               # Architecture Decision Records (ADRs 0001 - 0012)
│   ├── db/                # Scripts SQL y esquema de PostgreSQL (001_esquema_inicial.sql)
│   └── api-contract.md    # Contrato de endpoints de la API
└── tests/                 # Pruebas unitarias xUnit + Moq
```

---

## 🚀 Configuración y Puesta en Marcha

### Requisitos Previos

1. **.NET 9 SDK** y **.NET 10 SDK** instalados.
2. **Carga de trabajo .NET MAUI**:
   ```bash
   dotnet workload install maui
   ```
3. Dispositivo físico Android con depuración USB habilitada o emulador de Android.

---

### Paso 1. Configuración de Secretos de la API (User Secrets)

El backend utiliza **PostgreSQL en Supabase** a través del Session Pooler IPv4 (puerto `5432`). Por seguridad académica y mejores prácticas, las credenciales **nunca se suben a Git**.

Para configurar tu entorno de desarrollo, ubícate en la carpeta de la API y ejecuta:

```bash
cd src/BarberTurno.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=aws-0-us-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<tu_project_ref>;Password=<tu_contraseña>;SSL Mode=Require"
```

> [!TIP]
> Puedes consultar el `UserSecretsId` compartido en `src/BarberTurno.Api/BarberTurno.Api.csproj` o pedir la cadena exacta de conexión a tus compañeros de equipo por canal privado.
> El esquema inicial y las políticas RLS están versionados en `docs/db/001_esquema_inicial.sql`.

---

### Paso 2. Ejecutar la API Backend

Inicia la API en el puerto 5000:

```bash
dotnet run --project src/BarberTurno.Api --urls "http://127.0.0.1:5000"
```

* Swagger UI disponible en: `http://127.0.0.1:5000/`
* Diagnóstico de conexión (solo en desarrollo): `http://127.0.0.1:5000/api/diagnostico`

---

### Paso 3. Conectar Dispositivo Android con `adb reverse`

La app móvil está configurada en `AppConstants.cs` para apuntar a `http://127.0.0.1:5000/api/`. Para que tu teléfono físico o emulador acceda al puerto 5000 de tu PC sin problemas de firewall o red Wi-Fi:

```bash
adb reverse tcp:5000 tcp:5000
```

*(Si usas emulador estándar de Android Studio sin adb reverse, puedes usar `http://10.0.2.2:5000/api/`).*

---

### Paso 4. Ejecutar la Aplicación Móvil (.NET MAUI)

Compila y corre en Android:

```bash
dotnet build src/BarberTurno/BarberTurno.csproj -t:Run -f net10.0-android
```

O abre la solución en Visual Studio 2022, selecciona el proyecto `BarberTurno` como proyecto de inicio y pulsa **F5**.

---

### Paso 5. Ejecutar Pruebas Unitarias

Ejecuta el conjunto de pruebas automáticas con xUnit y Moq:

```bash
dotnet test tests/BarberTurno.Tests/BarberTurno.Tests.csproj
```

---

## 🔐 Usuarios y Credenciales de Prueba

Cuentas sembradas en la base de datos de Supabase:

| Rol | Usuario / Teléfono | Contraseña | Nombre |
| :--- | :--- | :--- | :--- |
| **Cliente** | `cliente1` | `123456` | Carlos Pérez |
| **Barbero** | `barbero1` | `123456` | Juan Barber |
| **Barbero** | `barbero2` | `123456` | Pedro Estilos |
