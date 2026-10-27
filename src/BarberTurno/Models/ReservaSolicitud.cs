// Responsable: M08
// Datos que el ViewModel entrega al servicio de reserva.
namespace BarberTurno.Models;

public class ReservaSolicitud
{
    public int BarberoId { get; set; }
    public int ServicioId { get; set; }
    public DateTime Inicio { get; set; }
}

