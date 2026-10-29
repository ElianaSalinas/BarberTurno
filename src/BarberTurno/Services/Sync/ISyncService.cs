// Responsable: M07
// Sincronización con la API y cola de reservas pendientes.
namespace BarberTurno.Services.Sync;

public interface ISyncService
{
    Task SincronizarCatalogosAsync();
    Task SincronizarPendientesAsync();
    event EventHandler<Guid>? ReservaRechazada;
}