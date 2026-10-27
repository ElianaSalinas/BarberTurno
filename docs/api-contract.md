# Contrato de la API (acordar y congelar en la semana 1: M03, M04, M05)

Base: `https://<servidor>/api` · Autenticación: `Authorization: Bearer <JWT>` salvo login · Fechas en UTC (ISO 8601).

| Método | Ruta | Descripción | Respuestas |
|---|---|---|---|
| POST | /auth/login | LoginRequestDto -> LoginResponseDto | 200, 401 |
| GET | /barberos | Barberos con ServicioIds | 200 |
| GET | /servicios | Servicios | 200 |
| GET | /disponibilidad?barberoId&servicioId&fecha | Lista de SlotDto libres | 200 |
| GET | /citas | Citas del usuario autenticado (cliente: las suyas; barbero: su agenda) | 200 |
| POST | /citas | CrearCitaRequestDto (Id = clave de idempotencia) | 201 creada, 200 si el Id ya existe (reintento), **409 hora ocupada**, 400 inválida |
| POST | /citas/{id}/cancelar | Cancela (opcional) | 200, 404 |

Reglas: la API es la autoridad de la disponibilidad; el 409 debe incluir un mensaje legible.
