# ADR-0012: Configuración y secretos; hosting de la API

- **Estado:** Aceptado
- **Fecha:** 2026-10-27 (Actualizado: 2026-10-30)
- **Responsable(s):** Módulo 6 (Cliente de API y Hosting)

## Contexto
El backend de *BarberTurno* (construido con ASP.NET Core) debe estar alojado en un entorno accesible por la aplicación móvil .NET MAUI tanto durante la fase de desarrollo local (emuladores y dispositivos físicos) como en el despliegue final para la defensa y uso en producción.

Requerimientos clave:
1. Ninguna credencial de base de datos, clave de cifrado JWT ni URLs absolutas deben quedar fijas (*hardcoded*) en el código fuente ni expuestas en repositorios públicos.
2. La API debe ser accesible por clientes móviles que operan desde distintos orígenes y redes (requiriendo soporte de CORS y políticas de seguridad adecuadas).
3. Comunicación obligatoria por canal seguro cifrado (**HTTPS**) en producción.
4. Soporte ágil para desarrollo local mediante emulador de Android (red `10.0.2.2`) y clientes de escritorio (`localhost`).
5. Persistencia centralizada en la nube con PostgreSQL (Supabase) y contenedorización reproducible.

## Decisión

1. **Gestión de Configuración y Secretos por Capas:**
   - En el servidor (`BarberTurno.Api`), la configuración base no sensible reside en `appsettings.json` (emisor y audiencia JWT).
   - Las credenciales sensibles (`ConnectionStrings:DefaultConnection` apuntando a PostgreSQL/Supabase y `Jwt:Key` de 256 bits) se gestionan mediante **Variables de Entorno** del sistema en el entorno de hosting y el gestor de secretos de .NET (`dotnet user-secrets`) durante el desarrollo local, garantizando cero secretos en el repositorio Git.
   - En la aplicación móvil (.NET MAUI), la dirección base de la API se abstrae en `AppConstants.ApiBaseUrl`, seleccionando automáticamente el endpoint local o el dominio público según la compilación.

2. **CORS (Cross-Origin Resource Sharing):**
   - Se configuró una política abierta `"AllowAll"` en el pipeline de ASP.NET Core (`Program.cs`) que autoriza cualquier encabezado, origen y método HTTP, garantizando que clientes Web o híbridos puedan consumir la API sin bloqueos de navegador.

3. **Estrategia de Hosting de la API (Render con Docker):**
   - La API está empaquetada como un contenedor reproducible mediante **Docker** (`Dockerfile` y `.dockerignore`), desplegado y alojado en **Render** con soporte HTTPS automático y certificados TLS.
   - En Android, el tráfico HTTP sin cifrar (`usesCleartextTraffic="true"`) se mantiene temporalmente para habilitar las pruebas locales en emuladores (`10.0.2.2:5000`), quedando el cierre del tráfico sin cifrar pendiente de aplicar en la versión final con TLS estricto.

4. **Base de Datos Centralizada (PostgreSQL en Supabase):**
   - Se utiliza PostgreSQL administrado en **Supabase** (usando el Connection Pooler transaccional en puerto 5432 o 6543) como base de datos de producción compartida, con migraciones y sembrado inicial automatizado en el arranque de la API.
   - SQLite se preserva localmente en la app móvil como caché offline (sincronizada vía `SyncService`) o como alternativa de desarrollo aislado con `"Database": { "UseLocalSqlite": true }`.

5. **Documentación Interactiva con Swagger/OpenAPI:**
   - Se integró `Swashbuckle.AspNetCore` configurado para ejecutarse en la raíz (`/`), permitiendo al equipo probar endpoints y la autenticación Bearer Token interactivamente.

## Alternativas consideradas

- **Quemar la clave secreta y la URL directamente en constantes de C#:**
  - *Rechazada terminantemente:* Viola las buenas prácticas de seguridad de software, impide cambiar de entorno sin recompilar y expondría secretos si el repositorio se hace público.
- **SQLite exclusivo en el servidor de producción:**
  - *Descartada para hosting distribuido:* SQLite local en contenedores de Render es efímero (se pierde al reiniciar el contenedor). Se adoptó PostgreSQL en Supabase para garantizar persistencia multi-instancia y transaccionalidad concurrente.

## Consecuencias

- **Ventajas:**
  - Despliegue automatizado, portátil y seguro mediante Docker en Render.
  - Base de datos centralizada resiliente con respaldo en Supabase.
  - Cero secretos expuestos en Git; cumplimiento total de las directivas de seguridad.
- **Riesgos y Mitigación:**
  - El tráfico HTTP sin cifrar local (`CleartextTraffic`) debe desactivarse antes de la entrega final. Mitigado mediante configuración condicional en el manifiesto de Android.
