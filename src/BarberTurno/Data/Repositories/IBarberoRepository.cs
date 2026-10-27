using BarberTurno.Models;
// Responsable: M02
// Contrato del repositorio de barberos.
namespace BarberTurno.Data.Repositories;

public interface IBarberoRepository
{
    Task<List<Barbero>> GetAllAsync();
    Task<Barbero?> GetByIdAsync(int id);
    /// <summary>Reemplaza el catálogo local con lo recibido de la API (barberos + relación con servicios).</summary>
    Task ReplaceAllAsync(IEnumerable<Barbero> barberos, IEnumerable<BarberoServicio> relaciones);
}

