-- ============================================================================
-- 003 - Relleno de CitaBloques para las citas ya existentes
-- Motivo: a partir de este cambio la API reserva, por cada cita, sus bloques de 5 min en "CitaBloques"
--         (índice único por barbero y bloque = garantía contra reservas simultáneas, ADR-0008).
--         Las citas creadas ANTES no tienen bloques; sin ellos, una reserva nueva podría solaparse con ellas.
-- No cambia el esquema (la tabla y el índice único ya existen en 001); solo agrega filas.
-- Estado: ejecutar UNA VEZ en Supabase antes de desplegar la API. Es idempotente (ON CONFLICT DO NOTHING).
-- ============================================================================

INSERT INTO "CitaBloques" ("BarberoId", "BloqueInicioUtc", "CitaId")
SELECT c."BarberoId", gs, c."Id"
FROM "Citas" c
CROSS JOIN LATERAL generate_series(
    date_trunc('hour', c."InicioUtc") + (floor(extract(minute FROM c."InicioUtc") / 5) * 5) * interval '1 minute',
    c."FinUtc" - interval '1 microsecond',
    interval '5 minutes'
) AS gs
WHERE c."Estado" NOT IN ('Cancelada', 'Rechazada')
ON CONFLICT DO NOTHING;
