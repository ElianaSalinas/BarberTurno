using BarberTurno.Models;
// Responsable: M02
// Contrato del repositorio de servicios.
namespace BarberTurno.Data.Repositories;

public interface IServicioRepository
{
    Task<List<Servicio>> GetAllAsync();
    Task<List<Servicio>> GetByBarberoAsync(int barberoId);
    Task<Servicio?> GetByIdAsync(int id);
    Task ReplaceAllAsync(IEnumerable<Servicio> servicios);
}

