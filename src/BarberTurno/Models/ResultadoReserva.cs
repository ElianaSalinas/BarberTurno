// Responsable: M08
// Resultado de intentar reservar.
namespace BarberTurno.Models;

public enum ResultadoReservaTipo { Confirmada, PendienteSinConexion, HoraOcupada, Invalida, Error }

public class ResultadoReserva
{
    public ResultadoReservaTipo Tipo { get; set; }
    public Cita? Cita { get; set; }
    public string? Mensaje { get; set; }
}

