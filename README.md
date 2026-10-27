# BarberTurno

Aplicación móvil de reservas para una barbería (.NET MAUI, MVVM, SQLite + API). Práctica integradora ISW-126, UCE.

## Estructura
```
BarberTurno/
├─ src/
│  ├─ BarberTurno/        App MAUI (Models, Data, Dtos, Mappers, Services, ViewModels, Views, Controls, Helpers)
│  └─ BarberTurno.Api/    API ASP.NET Core (el servidor es la autoridad de la disponibilidad)
├─ tests/                 Pruebas (opcional)
└─ docs/                  ADR, contrato de la API, diseño, reparto
```

## Puesta en marcha (líderes, una sola vez)

**Paso 1. Crear el proyecto MAUI con la plantilla** (versión del laboratorio), en una carpeta temporal:
```
dotnet new maui -n BarberTurno -o tmp/BarberTurno
```
**Paso 2.** Copiar a `src/BarberTurno/` los archivos de plataforma generados (`BarberTurno.csproj`, `Platforms/`,
`Resources/` (Fonts, Images, Raw, Splash, AppIcon, Styles/Colors.xaml y Styles.xaml), `Properties/`) SIN sobrescribir
los archivos de este repositorio (App.xaml(.cs), AppShell.xaml(.cs), MauiProgram.cs, Resources/Styles/AppTheme.xaml).
No copiar `MainPage.xaml(.cs)`.

**Paso 3. Paquetes NuGet** (dentro de `src/BarberTurno`):
```
dotnet add package CommunityToolkit.Mvvm
dotnet add package sqlite-net-pcl
dotnet add package SQLitePCLRaw.bundle_green
dotnet add package Plugin.LocalNotification      # recordatorios (M06)
dotnet add package CommunityToolkit.Maui         # opcional
```
**Paso 4. Solución:**
```
dotnet new sln -n BarberTurno
dotnet sln add src/BarberTurno/BarberTurno.csproj src/BarberTurno.Api/BarberTurno.Api.csproj
```
Verificar que el `RootNamespace` del proyecto MAUI sea `BarberTurno`. Compilar, hacer commit inicial y crear ramas.

## API
Se aloja en un servidor (ver PDF de reparto, sección Hosting). En el emulador de Android, `localhost` del PC es `10.0.2.2`.

## Usuarios de prueba
TODO(M03): documentar aquí al menos un cliente y un barbero (datos de ejemplo, nunca credenciales reales).

## Reglas de Git
Rama por tarea (`feature/m05-api-client`), Pull Request revisado por otro integrante, mensajes claros,
commits de TODOS, **cero secretos en el repositorio**.
