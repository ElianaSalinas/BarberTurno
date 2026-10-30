using BarberTurno.Models;

// Responsable: M04 (Repositorios)
// Contrato del repositorio de servicios (corte, barba, etc.). Solo acceso a datos.
namespace BarberTurno.Data.Repositories;

public interface IServicioRepository
{
    // Todos los servicios guardados, ordenados por nombre
    Task<List<Servicio>> GetAllAsync();


    // Servicios que ofrece un barbero (según la relación BarberoServicio), ordenados por nombre
    Task<List<Servicio>> GetByBarberoAsync(int barberoId);


    // Servicio por Id, o <c>null</c> si no existe. Lo usa la reserva para calcular Fin = Inicio + DuracionMin
    Task<Servicio?> GetByIdAsync(int id);


    // Inserta o actualiza un servicio por su Id
    Task UpsertAsync(Servicio servicio);


    // Reemplaza el catálogo completo por lo recibido de la API. Atómico
    Task ReplaceAllAsync(IEnumerable<Servicio> servicios);


    // Elimina un servicio y sus relaciones con barberos
    Task DeleteAsync(int id);


    // Borra todos los servicios (cierre de sesión)
    Task DeleteAllAsync();
}