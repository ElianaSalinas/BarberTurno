# BarberTurno.Api

Secretos (clave JWT, cadena de conexión de producción) NUNCA en el repositorio:
`dotnet user-secrets` en desarrollo y variables de entorno en el servidor.
Contrato de endpoints: `docs/api-contract.md`.

## Configuración obligatoria (la API no arranca sin ella)

| Dato | Desarrollo (User Secrets) | Servidor (variable de entorno) |
|---|---|---|
| Cadena de conexión | `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Password=..."` | `ConnectionStrings__DefaultConnection` |
| Clave de firma JWT (mínimo 32 caracteres) | `dotnet user-secrets set "Jwt:Key" "<cadena aleatoria>"` | `Jwt__Key` |

Si la clave JWT falta o tiene menos de 32 caracteres, la API se detiene al iniciar con un mensaje explicativo.
`appsettings.json` solo contiene valores no secretos (emisor, audiencia, duración).
