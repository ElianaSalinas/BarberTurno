# ADR-0005: Capa de red y traducción de datos

- **Estado:** Aceptado
- **Fecha:** 2026-10-27
- **Responsable(s):** Módulo 6 (Cliente de API y Hosting) y Módulo 2 (Modelos y DTOs)

## Contexto
La aplicación móvil *BarberTurno* desarrollada en .NET MAUI requiere comunicarse de forma asíncrona y segura con un backend centralizado en ASP.NET Core para autenticación, obtención del catálogo de barberos y servicios, consulta de franjas horarias disponibles y creación/cancelación de reservas.

En entornos móviles, las conexiones de red son inestables, intermitentes y sujetas a latencia o caídas abruptas. Además, el backend puede rechazar operaciones por razones de negocio esperadas (por ejemplo, código `409 Conflict` cuando otro usuario reserva la misma hora simultáneamente).

Era necesario responder a:
1. ¿Cómo estructurar las llamadas HTTP y gestionar el ciclo de vida del cliente de red?
2. ¿Cómo propagar los resultados y fallos a los ViewModels sin lanzar excepciones descontroladas?
3. ¿Cómo independizar las entidades internas de base de datos respecto a los contratos de intercambio?
4. ¿Qué archivos deben modificarse si la API cambia o renombra un campo?

## Decisión

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

## Alternativas consideradas

- **Refit / RestEase (Generación automática de clientes HTTP):**
  - *Ventaja:* Menos código manual de `HttpClient`.
  - *Desventaja:* Agrega dependencias de terceros y metaprogramación que pueden presentar incompatibilidades con AOT y trimming en ciertas plataformas móviles de .NET MAUI. Se prefirió `HttpClient` nativo con `System.Text.Json`.
- **Lanzar excepciones tradicionales (`HttpRequestException`) hacia la UI:**
  - *Rechazada:* Obligaría a que cada ViewModel implemente múltiples bloques `catch`, degradando el rendimiento en .NET MAUI y complicando la respuesta ante cortes de red o franjas ocupadas.

## Consecuencias

- **Ventajas:**
  - Código desacoplado, testeable y tolerante a fallos de conectividad.
  - Experiencia de usuario consistente gracias a mensajes de error unificados y detección del estado offline.
  - Facilidad de mantenimiento: el contrato API está centralizado en DTOs.
- **Costos y Mitigación:**
  - Requiere mantener los DTOs sincronizados entre la solución cliente y la API (mitigado mediante `api-contract.md` y pruebas de integración).
