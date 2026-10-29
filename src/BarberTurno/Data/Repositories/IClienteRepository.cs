using BarberTurno.Models;

// Responsable: M04 (Repositorios)
// Contrato del repositorio de clientes. El cliente ve su propio registro; el barbero guarda los
// clientes de su agenda para mostrar nombres y teléfono sin conexión.
namespace BarberTurno.Data.Repositories;

public interface IClienteRepository
{
    // Cliente por Id, o <c>null</c> si no existe localmente
    Task<Cliente?> GetByIdAsync(int id);


    // Clientes cuyos Id están en la lista (p. ej. los de la agenda del día), ordenados por nombre. Los Id repetidos se ignoran y los que no existen localmente simplemente no aparecen
    Task<List<Cliente>> GetByIdsAsync(IEnumerable<int> ids);


    // Todos los clientes guardados, ordenados por nombre
    Task<List<Cliente>> GetAllAsync();


    // Inserta o actualiza un cliente por su Id
    Task UpsertAsync(Cliente cliente);


    // Inserta o actualiza varios clientes en una sola transacción
    Task UpsertRangeAsync(IEnumerable<Cliente> clientes);


    // Borra todos los clientes (cierre de sesión)
    Task DeleteAllAsync();
}