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
   - En el servidor (`BarberTurno.Api`), la configuración base reside en `appsettings.json` (cadena de conexión de SQLite `Data Source=barberturno.db`, emisor y audiencia JWT).
   - Las claves críticas de firma (`Jwt:Key`) y credenciales de producción se gestionan mediante **Variables de Entorno** del sistema o el gestor de secretos de .NET (`dotnet user-secrets`), evitando subirlas al control de versiones Git.
   - En la aplicación móvil (.NET MAUI), la dirección base de la API se abstrae en `AppConstants.ApiBaseUrl`, seleccionando automáticamente `http://10.0.2.2:5000/api/` para el emulador de Android y `http://localhost:5000/api/` para Windows/escritorio, permitiendo apuntar dinámicamente al dominio HTTPS de producción al momento del despliegue.

2. **CORS (Cross-Origin Resource Sharing):**
   - Se configuró una política abierta `"AllowAll"` en el pipeline de ASP.NET Core (`Program.cs`) que autoriza cualquier encabezado, origen y método HTTP, garantizando que clientes Web o híbridos puedan consumir la API sin bloqueos de navegador.

3. **Estrategia de Hosting de la API:**
   - La API está empaquetada como un servicio autocontenido y liviano de ASP.NET Core, apto para ser publicado en servicios cloud con soporte HTTPS gratuito y certificados TLS automáticos (como Azure App Service, Render, Railway o VPS con Nginx/Caddy y Let's Encrypt).
   - En la versión final, se desactiva el tráfico HTTP sin cifrar (`CleartextTraffic`) en la app móvil y se enruta todo el tráfico a través del endpoint público HTTPS documentado.

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
