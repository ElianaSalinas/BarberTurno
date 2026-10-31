# ADR-0012: Configuración y secretos; hosting de la API

- **Estado:** Aceptado
- **Fecha:** 2026-10-27
- **Responsable(s):** Módulo 6 (Cliente de API y Hosting)

## Contexto
El backend de *BarberTurno* (construido con ASP.NET Core) debe estar alojado en un entorno accesible por la aplicación móvil .NET MAUI tanto durante la fase de desarrollo local (emuladores y dispositivos físicos) como en el despliegue final para la defensa y uso en producción.

Requerimientos clave:
1. Ninguna credencial de base de datos, clave de cifrado JWT ni URLs absolutas deben quedar fijas (*hardcoded*) en el código fuente ni expuestas en repositorios públicos.
2. La API debe ser accesible por clientes móviles que operan desde distintos orígenes y redes (requiriendo soporte de CORS y políticas de seguridad adecuadas).
3. Comunicación obligatoria por canal seguro cifrado (**HTTPS**) en la versión final de entrega.
4. Soporte sencillo para desarrollo local mediante emulador de Android (red `10.0.2.2`) y clientes de escritorio (`localhost`).

## Decisión

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

## Alternativas consideradas

- **Quemar la clave secreta y la URL directamente en constantes de C#:**
  - *Rechazada terminantemente:* Viola las buenas prácticas de seguridad de software, impide cambiar de entorno sin recompilar y expondría secretos si el repositorio se hace público.
- **Base de datos remota pesada (SQL Server o PostgreSQL remoto durante desarrollo inicial):**
  - *Descartada en fase inicial:* Se optó por **SQLite** con Entity Framework Core en el servidor, ya que es liviano, portátil, no requiere instalar servicios de base de datos externos para ejecutar los tests de integración y puede migrarse a PostgreSQL/SQL Server fácilmente cambiando el proveedor en `Program.cs`.

## Consecuencias

- **Ventajas:**
  - Despliegue ágil, portátil y seguro.
  - Integración sin fricción para los desarrolladores del equipo en cualquier plataforma (Windows, Android, Mac).
  - Cumplimiento de la regla de oro: cero secretos sensibles en el repositorio de Git.
- **Riesgos y Mitigación:**
  - El emulador de Android requiere `10.0.2.2` en lugar de `localhost`. Mitigado mediante la directiva condicional `#if ANDROID` en `AppConstants.cs`.
- **Costo del hosting gratuito:** el servicio de Render se suspende tras un rato sin uso y la primera petición tarda hasta un minuto en despertarlo; la app lo trata como un tiempo de espera normal (15 s de *timeout* y reintento al volver a abrir).
