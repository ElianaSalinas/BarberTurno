# ADR-0008: Cómo se evita que dos citas queden a la misma hora

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M04 (Jeifferson) y M08

## Contexto
Es el reto central de la práctica: dos citas no pueden coincidir para el mismo barbero. Parece trivial, pero hay tres escenarios difíciles: (a) dos clientes reservan la misma hora casi a la vez; (b) un teléfono sin conexión reserva una hora que otro ya tomó; (c) un error de programación intenta guardar dos citas iguales. Comprobar la disponibilidad en el teléfono antes de reservar **no es suficiente**: esa comprobación es una foto vieja y no ve lo que ocurre en otros dispositivos. La garantía debe estar donde se ve todo y donde la base de datos pueda hacerla cumplir.

## Decisión
Defensa en profundidad; **la autoridad es el servidor** (ADR-0004) y la última palabra la tiene una restricción de base de datos:

1. **Ofrecer solo horas libres (comodidad, no garantía).** `AvailabilityService` muestra únicamente franjas libres según la duración del servicio.
2. **Comprobación en el servidor, dentro de una transacción.** `POST /api/citas` rechaza con `409` si existe una cita activa que cumpla `existente.Inicio < nuevo.Fin && existente.Fin > nuevo.Inicio`.
3. **Restricción única que cierra la carrera.** Cada cita reserva en `CitaBloques` sus bloques de 5 min; la tabla tiene un **índice único `(BarberoId, BloqueInicioUtc)`**. Los bloques se insertan en la **misma transacción** que la cita. Si dos peticiones pasan a la vez la comprobación del paso 2, la segunda choca con el índice, la base de datos lanza una violación de unicidad y la API la traduce a `409`. Cancelar una cita **libera** sus bloques.
4. **Idempotencia.** El cliente genera el `Id` (Guid) de la cita; repetir el envío (reintento tras perder la red) devuelve la misma cita en vez de duplicarla.
5. **En el teléfono.** Índice único parcial `UX_Cita_BarberoId_Inicio_Activa` sobre `(BarberoId, Inicio)` para citas activas (rechazadas y canceladas no bloquean), y `ExisteSolapamientoAsync` para los solapes parciales. Un choque se traduce a `SlotOcupadoException`.
6. **Reserva sin conexión.** Queda `Pendiente`; al sincronizar, un `409` la pasa a `Rechazada` y se avisa al cliente (ver ADR-0004).
7. **Datos previos.** `docs/db/003_citabloques_relleno.sql` crea los bloques de las citas anteriores al cambio.

## Alternativas consideradas
- **Solo comprobar en la aplicación:** rechazado; no cubre la concurrencia ni el modo sin conexión.
- **Solo "consultar y luego insertar" en el servidor:** es lo que existía primero. Con aislamiento *Read Committed*, dos transacciones pueden pasar la consulta y ambas insertar; el hueco es pequeño pero real.
- **Aislamiento `SERIALIZABLE` con reintentos, o bloqueos `SELECT … FOR UPDATE`:** también correctos, pero más costosos, dependen del motor y hay que gestionar reintentos y bloqueos muertos.
- **Restricción de exclusión de PostgreSQL (`EXCLUDE USING gist` sobre rangos de tiempo):** la solución más elegante y exacta; se descartó por requerir la extensión `btree_gist`, mapeo especial en EF Core y no funcionar en SQLite, que usamos en pruebas locales. Es la mejora natural si se migra a migraciones formales.

## Consecuencias
- **Ventajas:** ni dos peticiones simultáneas ni un error de programación pueden dejar dos citas activas solapadas en la misma hora: la base de datos lo impide. El conflicto llega al usuario como un mensaje claro.
- **Costos:** una tabla auxiliar y varias filas por cita; hay que mantenerla coherente al cancelar (hecho) y rellenarla para datos antiguos (script 003). Los bloques se redondean hacia abajo a 5 min, por lo que una hora no alineada puede producir un falso conflicto de pocos minutos, nunca un solape sin detectar.
- **Riesgos y mitigación:** el índice local solo detecta el mismo inicio; los solapes parciales dependen de `ExisteSolapamientoAsync` y, sobre todo, del servidor. No hay aún una prueba automatizada de concurrencia contra la API (las pruebas unitarias cubren el solape local y los mapeadores); es la prueba pendiente más importante antes de la defensa: lanzar dos `POST` simultáneos y verificar un solo `201` y un `409`.
