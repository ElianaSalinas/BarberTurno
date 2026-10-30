# Documentación Técnica y Diagnóstico de Arquitectura · Módulo 6
**Proyecto:** BarberTurno (ISW-126 - Desarrollo de Aplicaciones Móviles I)  
**Autor:** Ramón  
**Módulo:** Módulo 6 – Cliente de la API y Servidor Alojado  
**Fecha:** 27 de Octubre, 2026  

---

## 1. Resumen Ejecutivo y Alcance del Módulo

De acuerdo con el documento oficial de especificación y arquitectura (*BarberTurno · Plan de trabajo detallado*), al **Módulo 6** le corresponde garantizar el puente de comunicación entre la aplicación móvil cliente (.NET MAUI) y los servicios centralizados en la nube mediante un backend REST en **ASP.NET Core**.

El alcance asignado comprende dos vertientes críticas:

1. **Cliente de Red en .NET MAUI (`src/BarberTurno/Services/Api/`)**:
   - Definición e implementación del contrato HTTP tipado (`IBarberTurnoApi`, `BarberTurnoApi`).
   - Abstracción de respuestas, errores de servidor y estados de conectividad mediante `ApiResult<T>`.
   - Inyección del token Bearer (JWT) en peticiones autenticadas.
   - Parametrización dinámica de la URL base del servicio sin credenciales o rutas quemadas (*hardcoded*).
   - Registro en el contenedor de dependencias (`ApiServiceRegistration.cs`).

2. **Servidor Backend y Persistencia Centralizada (`src/BarberTurno.Api/`)**:
   - Configuración de la API REST (`Program.cs`, CORS, autenticación JWT Bearer, Swagger).
   - Persistencia relacional propia mediante **Entity Framework Core** y SQLite (`ApiDbContext`).
   - Modelado de dominio del servidor (`UsuarioEntity`, `BarberoEntity`, `ServicioEntity`, `BarberoServicioEntity`).
   - Carga inicial de datos de prueba (*Seed Data*) con contraseñas hasheadas en **BCrypt**.
   - Controladores base: `AuthController`, `BarberosController` y `ServiciosController`.
   - Soporte para hosting accesible vía HTTPS.

3. **Arquitectura y Documentación de Decisiones**:
   - **ADR-0005:** Capa de red y traducción de datos.
   - **ADR-0012:** Configuración y secretos; hosting de la API.

---

## 2. Diagnóstico del Estado Inicial del Repositorio

Tras realizar una inspección exhaustiva del código fuente, el árbol de directorios y ejecutar la compilación de la solución (`BarberTurno.sln`), se identificaron las siguientes condiciones de partida:

### 2.1. Inconsistencias en el Proyecto MAUI Móvil (`BarberTurno.csproj`)
- **Problema detectado:** El archivo de proyecto cliente contenía referencias a paquetes NuGet diseñados exclusivamente para entornos de servidor ASP.NET Core:
  - `Microsoft.AspNetCore.Authentication.JwtBearer` (v10.0.14)
  - `Microsoft.EntityFrameworkCore` (v10.0.14)
  - `Microsoft.EntityFrameworkCore.Sqlite.Design` (v1.1.6)
- **Impacto:** Provocaba fallos de compilación (`NETSDK1082`) en plataformas móviles (Android, iOS, MacCatalyst), ya que el runtime de MAUI no incluye el paquete compartido `Microsoft.AspNetCore.App`.
- **Por qué fue así la corrección:** Dichas dependencias pertenecen estrictamente al proyecto backend `BarberTurno.Api`. La aplicación móvil solo debe interactuar con la red mediante `HttpClient` y almacenar sus datos locales mediante `sqlite-net-pcl`, manteniendo un desacoplamiento limpio entre cliente y servidor.

### 2.2. Estado de `BarberTurno.Api`
- **Problema detectado:** Los controladores `AuthController`, `BarberosController`, `ServiciosController`, `CitasController` y `DisponibilidadController` presentaban errores de sintaxis (`CS0246`, `CS0616`) por falta de directivas `using Microsoft.AspNetCore.Mvc;` y falta de configuración explícita en el SDK web.
- **Entidades de Dominio vacías:** Los modelos en `Domain/` (`BarberoEntity`, `UsuarioEntity`, `ServicioEntity`, etc.) se encontraban como esqueletos vacíos (`// TODO`).
- **Contexto de Base de Datos desconfigurado:** `ApiDbContext.cs` no heredaba de `Microsoft.EntityFrameworkCore.DbContext` ni definía los `DbSet<T>`.

### 2.3. Capa de Cliente API (`src/BarberTurno/Services/Api/`)
- `IBarberTurnoApi.cs` define el contrato esperado por los demás módulos del equipo (Módulo 5 Auth, Módulo 7 Disponibilidad/Reserva, Módulo 8 Sincronización).
- `BarberTurnoApi.cs` contenía un esqueleto sin implementar.
- `ApiResult.cs` provee la estructura requerida para tipar el resultado sin recurrir a excepciones no controladas.

---

## 3. Decisiones de Diseño y Arquitectura ("Por Qué Fue Así")

### 3.1. Patrón `ApiResult<T>` frente a Excepciones Tradicionales
- **Decisión:** El cliente HTTP encapsula todas las respuestas en un contenedor `ApiResult<T>` con propiedades:
  - `Exito`: bandera booleana.
  - `Datos`: payload deserializado de tipo `T`.
  - `CodigoHttp`: código de estado HTTP (ej. 409 Conflict para horas ocupadas, 401 Unauthorized para token inválido).
  - `SinConexion`: indicador de timeout o pérdida de conectividad física.
  - `Error`: mensaje legible amigable para presentar en UI.
- **Justificación:** Lanzar excepciones (`HttpRequestException`, `TimeoutException`) para flujos habituales de negocio (como una hora ya ocupada o una caída temporal de red en un móvil) degrada el rendimiento de .NET MAUI y ensucia la lógica de los ViewModels. `ApiResult<T>` permite a los ViewModels de los módulos 5, 7, 8 y 9 reaccionar funcionalmente con `if (resultado.SinConexion)` o `if (resultado.CodigoHttp == 409)`.

### 3.2. Autoridad Central en el Servidor (Single Source of Truth)
- **Decisión:** El servidor ASP.NET Core es la única entidad autorizada para confirmar o denegar reservas y validar la disponibilidad horaria.
- **Justificación:** En un entorno concurrente donde múltiples dispositivos móviles intentan agendar una cita simultáneamente, la validación local en el dispositivo es insuficiente. El servidor actúa como árbitro centralizado con restricciones transaccionales e índices únicos.

### 3.3. Separación estricta entre DTOs y Entidades de Base de Datos
- **Decisión:** Ni el cliente expone sus modelos SQLite a la API, ni el servidor expone sus entidades EF Core a los clientes. Se utiliza un contrato unificado basado en DTOs (`Dtos.cs` y `ApiDtos.cs`).
- **Justificación:** Si la estructura de las tablas de la base de datos cambia o se agregan campos internos (como el salt de la contraseña o marcas de auditoría), el contrato público de la API no sufre modificaciones de ruptura (*breaking changes*).

### 3.4. Autenticación Stateless basada en JWT (JSON Web Tokens)
- **Decisión:** Implementación de tokens JWT firmados con clave simétrica secreta en el endpoint `POST /api/auth/login`.
- **Justificación:** Permite que la API sea completamente desacoplada y escalable (*stateless*). El cliente MAUI guarda el token en el almacenamiento seguro de la plataforma (`SecureStorage` - Módulo 5) y lo envía en el encabezado `Authorization: Bearer <token>` en cada solicitud posterior.

### 3.5. Hashing de Contraseñas con BCrypt
- **Decisión:** Ninguna credencial de usuario se almacena en texto plano en la base de datos del servidor; se utiliza la biblioteca `BCrypt.Net-Next`.
- **Justificación:** Cumple con los estándares de seguridad de la industria, protegiendo las cuentas de clientes y barberos contra ataques de diccionario o filtraciones de la base de datos.

### 3.6. Gestión de Secretos y Configuración Externa
- **Decisión:** La URL base de la API y las claves secretas de firma JWT se extraen de archivos de configuración (`appsettings.json`, constantes configurables de entorno) y nunca se cablean estáticamente en el código fuente de los controladores o servicios.
- **Justificación:** Facilita cambiar entre el entorno de desarrollo local (ej. `localhost` o IP local de emulador Android `10.0.2.2`) y el entorno de producción en un servidor público HTTPS sin recompilar la aplicación.

---

## 4. Plan de Implementación por Fases

A continuación se detalla la secuencia de trabajo planificada para la ejecución del Módulo 6:

```
[Fase 1: Estabilización y Compilación]
  ├── Limpieza de dependencias en BarberTurno.csproj
  ├── Instalación de paquetes EF Core y JWT en BarberTurno.Api.csproj
  └── Corrección de directivas 'using' en controladores

[Fase 2: Persistencia del Servidor y Seed Data]
  ├── Definición de entidades: UsuarioEntity, BarberoEntity, ServicioEntity, BarberoServicioEntity
  ├── Implementación de ApiDbContext con relaciones y restricciones
  └── Carga de datos semilla (Seed Data con usuarios y servicios de prueba)

[Fase 3: Lógica de Controladores y Seguridad en la API]
  ├── Configuración de JWT Bearer y política CORS en Program.cs
  ├── AuthController: Validación de credenciales y emisión de token JWT
  ├── BarberosController: Consulta de barberos y sus servicios
  └── ServiciosController: Catálogo de servicios disponibles

[Fase 4: Cliente HTTP en la App Móvil (.NET MAUI)]
  ├── Implementación de BarberTurnoApi (HttpClient, JsonSerializer, Token Injection)
  ├── Manejo de Timeouts y resiliencia de red en ApiResult<T>
  └── Configuración e inyección de dependencias en ApiServiceRegistration.cs

[Fase 5: Documentación y Registros de Decisiones de Arquitectura (ADRs)]
  ├── Redacción y formalización de ADR-0005 (Capa de Red)
  └── Redacción y formalización de ADR-0012 (Hosting y Configuración de Secretos)
```

---

## 5. Conclusión y Próximos Pasos

La arquitectura diseñada garantiza una integración modular y robusta con los módulos adyacentes:
- Provee a **M05** la infraestructura para el manejo de sesión y tokens.
- Provee a **M07** y **M08** los contratos de red para disponibilidad y sincronización.
- Mantiene aisladas las responsabilidades evitando acoplamientos innecesarios.

El siguiente paso es la ejecución técnica de la **Fase 1**, asegurando la compilación exitosa y limpia de toda la solución.
