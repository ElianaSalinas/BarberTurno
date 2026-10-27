# BarberTurno

Aplicación móvil de reservas para una barbería desarrollada con **.NET MAUI**, arquitectura **MVVM**, persistencia local con **SQLite** y sincronización con una **API REST en ASP.NET Core**.

> Proyecto correspondiente a la práctica integradora **ISW-126**, Universidad Central del Este (**UCE**).

---

## 👥 Reparto de Responsabilidades y Módulos

A continuación se detalla la asignación de cada módulo y los módulos pendientes de asignación:

| Módulo | Responsable | Área / Funcionalidad | Carpetas / Componentes Clave | ADR Asociados |
| :--- | :--- | :--- | :--- | :--- |
| **M01** | **Eliana** | Arquitectura base, Shell, Inyección de Dependencias (DI), integración general y control de versiones | `App.xaml`, `AppShell`, `MauiProgram`, `Helpers/`, `Services/Navigation`, `Services/Dialogs`, `BaseViewModel` | [0001](docs/adr/0001-patron-de-presentacion.md), [0002](docs/adr/0002-navegacion-entre-perfiles-de-cliente-y-barber.md), [0007](docs/adr/0007-inyeccion-de-dependencias-y-ciclos-de-vida.md) |
| **M02** | **Brailyn** | Modelado de datos de dominio y persistencia local SQLite | `Models/`, `Data/` | [0003](docs/adr/0003-persistencia-local-con-sqlite.md) |
| **M03** | ⏳ *Pendiente* | API: Estructura base, autenticación y despliegue (Hosting) | `src/BarberTurno.Api/` (`Program.cs`, `Auth/`, `Barberos/`, `Servicios/`, `Domain/`, `Data/`) | [0012](docs/adr/0012-configuracion-y-secretos-hosting-de-la-api.md) |
| **M04** | **Jeifferson** | API: Disponibilidad y reservas (gestión de concurrencia en servidor) | Endpoints y servicios de citas y disponibilidad (`Citas/`, `Disponibilidad/`, `Services/`) | [0008](docs/adr/0008-como-se-evita-que-dos-citas-queden-a-la-misma.md) |
| **M05** | **Elian** | Capa de red del cliente móvil, DTOs y traductores (Mappers) | `Services/Api/`, `Dtos/`, `Mappers/` | [0005](docs/adr/0005-capa-de-red-y-traduccion-de-datos.md) |
| **M06** | ⏳ *Pendiente* | Autenticación en app, almacenamiento seguro (`SecureStorage`) y notificaciones locales | `Services/Auth/`, `Services/Notifications/`, `Views/Login/` | [0006](docs/adr/0006-autenticacion-y-manejo-de-la-sesion.md), [0009](docs/adr/0009-recordatorios-con-notificaciones-locales.md) |
| **M07** | **Jorge Cabrera** | Modo sin conexión (Offline) y sincronización con servidor | `Services/Network/`, `Services/Sync/` | [0004](docs/adr/0004-datos-y-sincronizacion-fuente-de-verdad-y-mod.md) |
| **M08** | ⏳ *Pendiente* | Flujo de reserva y consulta de disponibilidad (lado cliente) | `Services/Availability/`, `Services/Booking/`, pantallas de selección de hora y confirmación | [0008](docs/adr/0008-como-se-evita-que-dos-citas-queden-a-la-misma.md) |
| **M09** | **Albert Frank** | Interfaz de usuario (UI) del cliente y manejo de estados de pantalla | `Views/Cliente/`, `Views/Perfil/`, `Controls/StateView/`, `Converters/` | [0010](docs/adr/0010-manejo-de-estados-de-interfaz-y-errores.md) |
| **M10** | ⏳ *Pendiente* | Interfaz de usuario (UI) del barbero y soporte tema claro/oscuro | `Views/Barbero/`, `Resources/Styles/` | [0011](docs/adr/0011-tema-visual-claro-oscuro.md) |

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
│   │   ├── Helpers/       # Constantes y utilidades
│   │   ├── Mappers/       # Mapeo entre Modelos y DTOs
│   │   ├── Models/        # Modelos de dominio del cliente
│   │   ├── Platforms/     # Código específico por plataforma (Android, iOS, Windows)
│   │   ├── Resources/     # Recursos estáticos (estilos, fuentes, iconos, splash)
│   │   ├── Services/      # Lógica de negocio, API client, sync, auth
│   │   ├── ViewModels/    # Lógica de presentación (CommunityToolkit.Mvvm)
│   │   └── Views/         # Pantallas XAML (Cliente, Barbero, Auth)
│   └── BarberTurno.Api/   # Backend API REST (ASP.NET Core)
│       ├── Controllers/   # Controladores de endpoints
│       ├── Data/          # Contexto de base de datos del servidor
│       ├── Domain/        # Modelos de dominio del servidor
│       ├── Dtos/          # DTOs de entrada y salida
│       └── Services/      # Servicios de reserva y disponibilidad
├── docs/                  # Documentación técnica, ADRs y contrato de API
│   ├── adr/               # Architecture Decision Records (ADRs)
│   ├── api-contract.md    # Contrato de endpoints de la API
│   └── reparto.md         # Detalle del reparto de tareas
└── tests/                 # Pruebas unitarias y de integración
```

---

## 🚀 Cómo Iniciar el Proyecto

### Requisitos Previos

1. **.NET 9 SDK / .NET 10 SDK** instalado en el equipo.
2. **Carga de trabajo de .NET MAUI**:
   ```bash
   dotnet workload install maui
   ```
3. **IDE recomendado**:
   - [Visual Studio 2022](https://visualstudio.microsoft.com/) con la carga de trabajo *Desarrollo con .NET Multi-platform App UI (.NET MAUI)*.
   - O [Visual Studio Code](https://code.visualstudio.com/) con la extensión *.NET MAUI* y *C# Dev Kit*.
4. **Para emulación/ejecución**:
   - Emulador de Android configurado (vía Visual Studio o Android Studio) o dispositivo físico con depuración USB habilitada.
   - O máquina local Windows (Windows App SDK).

---

### Paso 1. Clonar el Repositorio

```bash
git clone https://github.com/<tu-usuario-o-organizacion>/BarberTurno.git
cd BarberTurno
```

---

### Paso 2. Restaurar Dependencias

Restaura los paquetes NuGet de toda la solución:

```bash
dotnet restore BarberTurno.sln
```

---

### Paso 3. Ejecutar la API Backend (`BarberTurno.Api`)

Abre una terminal y ejecuta el proyecto de la API:

```bash
dotnet run --project src/BarberTurno.Api
```

- La API se iniciará por defecto en las siguientes URLs locales:
  - **HTTPS:** `https://localhost:62200`
  - **HTTP:** `http://localhost:62201`
- Contrato detallado de los endpoints en [`docs/api-contract.md`](docs/api-contract.md).

> [!IMPORTANT]
> **Conexión desde el Emulador de Android:**
> El emulador de Android corre en una red virtual aislada. Para acceder al `localhost` de tu computadora desde el emulador de Android, debes utilizar la dirección IP:
> ```text
> http://10.0.2.2:62201
> ```

---

### Paso 4. Ejecutar la Aplicación Móvil (`BarberTurno`)

#### Opción A: Desde Visual Studio 2022 (Recomendada)
1. Abre el archivo `BarberTurno.sln` en Visual Studio.
2. Haz clic derecho sobre el proyecto `BarberTurno` en el Explorador de Soluciones y selecciona **Establecer como proyecto de inicio**.
3. En la barra superior, selecciona el dispositivo de destino (ejemplo: *Android Emulator - Pixel...* o *Windows Machine*).
4. Presiona **F5** (o el botón verde *Iniciar depuración*).

#### Opción B: Desde la Terminal (CLI)
- **Para ejecutar en Windows:**
  ```bash
  dotnet build src/BarberTurno -t:Run -f net10.0-windows10.0.19041.0
  ```
- **Para ejecutar en Android (con emulador activo):**
  ```bash
  dotnet build src/BarberTurno -t:Run -f net10.0-android
  ```

---

## 🌿 Flujo de Trabajo y Reglas de Git

Para mantener la estabilidad e integridad del repositorio:

1. **Ramas por módulo / tarea:**
   - Crear una rama específica para cada tarea basada en `master` o `main`:
     ```bash
     git checkout -b feature/m02-modelos-sqlite
     # o
     git checkout -b feature/m05-api-client
     ```
2. **Commits claros y descriptivos:**
   - Realizar commits atómicos con mensajes en presente y explicativos:
     ```bash
     git commit -m "M02: Crear entidades Cita, Barbero y Servicio para SQLite"
     ```
3. **Pull Requests (PR):**
   - Subir la rama a remoto (`git push origin feature/...`).
   - Crear un Pull Request y solicitar revisión a otro compañero antes de integrar a la rama principal.
4. **Cero Secretos en el Repositorio:**
   - Nunca subir contraseñas, cadenas de conexión reales o claves JWT al repositorio.
   - Utilizar `dotnet user-secrets` en desarrollo local y variables de entorno en producción.

---

## 🔐 Usuarios de Prueba

Para las pruebas locales en desarrollo (M03 y M06 documentarán las credenciales locales simuladas):

| Rol | Usuario / Correo | Contraseña | Notas |
| :--- | :--- | :--- | :--- |
| **Cliente** | `cliente.demo@barberturno.test` | `Cliente123!` | Cuenta de prueba predeterminada |
| **Barbero** | `barbero.demo@barberturno.test` | `Barbero123!` | Cuenta con permisos de barbero |
