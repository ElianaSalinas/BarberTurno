using BarberTurno.Data.Exceptions;
using BarberTurno.Models;

// Responsable: M04 (Repositorios)
// Contrato del repositorio de citas.

// Reglas:
//  - Solo acceso a datos. Las decisiones (si se puede reservar, qué hacer ante un rechazo) son de
//    BookingService (M07) y SyncService (M08).
//  - Ocupan hora todas las citas EXCEPTO las Rechazadas y Canceladas, que liberan su franja.
//    Es la misma regla del índice único filtrado de AppDatabase (M03): AppDatabase.EstadosQueLiberanHorario.
//  - Las fechas son hora local (el DtoMapper de M02 convierte desde/hacia UTC).
namespace BarberTurno.Data.Repositories;

public interface ICitaRepository
{
    // Cita por Id, o <c>null</c>
    Task<Cita?> GetByIdAsync(Guid id);


    // Todas las citas de un cliente, ordenadas por Inicio ascendente
    Task<List<Cita>> GetByClienteAsync(int clienteId);


    /// <summary>
    /// Citas del cliente con <c>Inicio &gt;= desde</c>, ascendente (la más cercana primero).
    /// No filtra por estado: "Mis citas" debe mostrar también Pendientes y Rechazadas.
    /// </summary>
    Task<List<Cita>> GetProximasClienteAsync(int clienteId, DateTime desde);


    // Citas del cliente con <c>Inicio &lt; desde</c>, descendente (la más reciente primero). No filtra por estado
    Task<List<Cita>> GetAnterioresClienteAsync(int clienteId, DateTime desde);


    // Citas de un barbero cuyo Inicio cae en <paramref name="dia"/>, ordenadas por hora. Todos los estados
    Task<List<Cita>> GetAgendaBarberoAsync(int barberoId, DateOnly dia);


    /// <summary>
    /// Historial de visitas de un cliente, descendente. Si <paramref name="barberoId"/> es null incluye
    /// todos los barberos; si <paramref name="antesDe"/> tiene valor, solo citas con Inicio anterior a esa fecha.
    /// <paramref name="skip"/>/<paramref name="take"/> permiten carga por páginas.
    /// </summary>
    Task<List<Cita>> GetHistorialClienteAsync(int clienteId, int? barberoId = null,
                                              DateTime? antesDe = null, int skip = 0, int take = 50);


    /// <summary>
    /// Citas con <c>PendienteSincronizar = true</c> (creadas sin conexión y aún no enviadas al servidor),
    /// la más antigua (por Inicio) primero. Para SyncService (M08).
    /// </summary>
    Task<List<Cita>> GetPendientesAsync();


    /// <summary>
    /// ¿Existe una cita del barbero que OCUPE hora (no Rechazada ni Cancelada) y se solape con [inicio, fin)?
    /// Regla: existente.Inicio &lt; fin y existente.Fin &gt; inicio (las citas contiguas no chocan).
    /// <paramref name="excluirId"/> sirve al reprogramar, para no chocar con la propia cita.
    /// </summary>
    Task<bool> ExisteSolapamientoAsync(int barberoId, DateTime inicio, DateTime fin, Guid? excluirId = null);


    /// <summary>Inserta una cita nueva tal como llega (incluido su PendienteSincronizar).</summary>
    /// <exception cref="SlotOcupadoException">El índice único rechazó la cita: el barbero ya tiene otra cita que ocupa esa hora.</exception>
    Task InsertAsync(Cita cita);


    /// <summary>
    /// Cambia el estado (y el motivo de rechazo) de una cita local, sin tocar PendienteSincronizar.
    /// Ejemplos: Confirmada → Atendida / Ausente (M10), → Cancelada. Devuelve <c>false</c> si la cita no existe.
    /// </summary>
    /// <exception cref="SlotOcupadoException">Al reactivar una cita cuya hora ya está ocupada por otra.</exception>
    Task<bool> ActualizarEstadoAsync(Guid id, EstadoCita estado, string? motivo = null);


    /// <summary>
    /// Registra la respuesta del servidor a una cita pendiente: guarda el estado que decidió el servidor
    /// (Confirmada o Rechazada, con su motivo) y pone <c>PendienteSincronizar = false</c>, en una sola operación.
    /// Para SyncService (M08). Devuelve <c>false</c> si la cita no existe.
    /// </summary>
    /// <exception cref="SlotOcupadoException">Si el estado recibido ocupa una hora ya tomada por otra cita local.</exception>
    Task<bool> MarcarSincronizadaAsync(Guid id, EstadoCita estado, string? motivo = null);


    /// <summary>
    /// Inserta o actualiza las citas recibidas del servidor, en una transacción.
    /// Se hace "UPDATE por Id; si no existe, INSERT" y nunca INSERT OR REPLACE, porque este último
    /// borraría en silencio la cita local que choque en (BarberoId, Inicio).
    /// Se aplican primero las citas que liberan hora (Rechazada/Cancelada), de modo que un lote
    /// "la mía fue rechazada + la del otro cliente quedó confirmada" funciona en cualquier orden.
    /// </summary>
    /// <exception cref="SlotOcupadoException">Si una cita del servidor choca con otra cita local distinta; no se aplica ningún cambio.</exception>
    Task UpsertRangeAsync(IEnumerable<Cita> citas);


    // Borra todas las citas (cierre de sesión)
    Task DeleteAllAsync();
}