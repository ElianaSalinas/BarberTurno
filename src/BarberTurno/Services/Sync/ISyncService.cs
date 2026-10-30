// Responsable: M07 / M04
// Sincronización con la API y cola de reservas pendientes.
namespace BarberTurno.Services.Sync;

public interface ISyncService
{
    Task SincronizarCatalogosAsync();
    Task SincronizarCitasAsync();
    Task SincronizarPendientesAsync();
    event EventHandler<Guid>? ReservaRechazada;
}
