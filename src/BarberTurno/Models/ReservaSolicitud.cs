// Responsable: M02
// Datos que el ViewModel entrega al servicio de reserva (no se persiste en SQLite).
namespace BarberTurno.Models;

/// <summary>
/// Parámetros necesarios para solicitar una reserva desde la UI hacia BookingService.
/// </summary>
public class ReservaSolicitud
{
    public int BarberoId { get; set; }
    public int ServicioId { get; set; }
    public DateTime Inicio { get; set; }
}
