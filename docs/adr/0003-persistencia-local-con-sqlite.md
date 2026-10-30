# ADR-0003: Persistencia local con SQLite

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M02 (Brailyn)

## Contexto
La aplicación debe recordar sus datos entre sesiones y permitir consultar barberos, servicios y citas sin señal. Las entidades obligatorias son Cliente, Barbero, Servicio y Cita (más la relación barbero-servicio). Se necesitaba un almacenamiento relacional, consultable por rangos de fecha (agenda del día, historial) y capaz de hacer cumplir restricciones únicas (una cita activa por barbero y hora de inicio).

## Decisión
1. **SQLite** con `sqlite-net-pcl` 1.11 y `SQLitePCLRaw.bundle_green`. Tablas: `Cliente`, `Barbero`, `Servicio`, `BarberoServicio` y `Cita`.
2. Una única conexión asíncrona compartida (`AppDatabase`, singleton), creada de forma diferida y protegida con `SemaphoreSlim`; se activa el modo **WAL** para leer mientras la sincronización escribe.
3. Esquema: `CreateTablesAsync` crea las tablas y añade columnas nuevas; los **índices** que los atributos no permiten (el parcial de citas activas y el único de barbero-servicio) se crean con SQL en una migración idempotente (`CREATE UNIQUE INDEX IF NOT EXISTS`) con versión de esquema.
4. Acceso solo mediante **repositorios** (`ICitaRepository`, etc., singletons), nunca con la conexión desde un ViewModel.
5. Fechas con `storeDateTimeAsTicks`. Convención: **`Cita.Inicio` y `Fin` se guardan en hora local**; el servidor y los DTO usan UTC y `DtoMapper` convierte en ambos sentidos.

## Alternativas consideradas
- **EF Core + SQLite:** migraciones y LINQ más completos, pero mayor tamaño, arranque más lento y fricción con recorte (*trimming*) en móvil; el esquema es pequeño.
- **LiteDB / Realm:** documentos u objetos sin SQL; no ofrecen índices únicos parciales ni consultas relacionales con la misma facilidad y el equipo ya conoce SQL.
- **`Preferences`/archivos JSON:** sirven para ajustes, no para consultar por rangos ni imponer unicidad.

## Consecuencias
- **Ventajas:** funciona sin conexión, restricción única local, consultas rápidas, dependencia pequeña.
- **Costos:** sqlite-net no tiene migraciones reales; los cambios de esquema se escriben a mano. Al guardar fechas como *ticks* se pierde el `Kind`: al leer, `DateTime` llega como `Unspecified`, por lo que **no se debe llamar `ToLocalTime()` sobre `Cita.Inicio`** (error que ya ocurrió en "Mis citas" y se corrigió).
- **Riesgos y mitigación:** una sola conexión serializa escrituras; el modo WAL y las consultas cortas lo mantienen fluido. El catálogo de prueba solo se siembra con la base vacía y la sincronización lo reemplaza.
