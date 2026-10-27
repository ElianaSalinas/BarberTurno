// Responsable: M07
// Sincronización con la API y cola de reservas pendientes.
namespace BarberTurno.Services.Sync;

public interface ISyncService
{
    /// <summary>Descarga barberos, servicios y citas y actualiza SQLite (el servidor manda).</summary>
    Task SincronizarCatalogosAsync();
    /// <summary>Envía en orden las citas PendienteConfirmacion; actualiza a Confirmada o Rechazada.</summary>
    Task SincronizarPendientesAsync();
    /// <summary>Se dispara cuando una reserva offline fue rechazada (para avisar al cliente).</summary>
    event EventHandler<Guid>? ReservaRechazada;
}

