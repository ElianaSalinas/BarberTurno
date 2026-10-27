using BarberTurno.Models;
// Responsable: M02
// Contrato del repositorio de clientes.
namespace BarberTurno.Data.Repositories;

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(int id);
    Task UpsertAsync(Cliente cliente);
}

