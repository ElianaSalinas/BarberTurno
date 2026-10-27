using BarberTurno.Models;
// Responsable: M02
// Contrato del repositorio de citas.
namespace BarberTurno.Data.Repositories;

public interface ICitaRepository
{
    Task<Cita?> GetByIdAsync(Guid id);
    Task<List<Cita>> GetByClienteAsync(int clienteId);
    Task<List<Cita>> GetAgendaBarberoAsync(int barberoId, DateOnly dia);        // ordenada por hora
    Task<List<Cita>> GetHistorialClienteAsync(int clienteId, int barberoId);
    Task<List<Cita>> GetPendientesAsync();                                       // para SyncService
    Task<bool> ExisteSolapamientoAsync(int barberoId, DateTime inicio, DateTime fin);
    /// <summary>Inserta; lanza SlotOcupadoException si el índice único lo rechaza.</summary>
    Task InsertAsync(Cita cita);
    Task ActualizarEstadoAsync(Guid id, EstadoCita estado, string? motivo = null);
    Task UpsertRangeAsync(IEnumerable<Cita> citas);
}

