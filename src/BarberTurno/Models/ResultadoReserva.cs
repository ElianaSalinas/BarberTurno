// Responsable: M02
// Resultado de intentar crear una reserva. Usado por M07 (BookingService) y M09 (UI cliente).
namespace BarberTurno.Models;

/// <summary>
/// Tipo de resultado al intentar reservar una cita.
/// Corresponde con los posibles retornos de BookingService (M07).
/// </summary>
public enum ResultadoReservaTipo
{
    Confirmada = 0,         // el servidor aceptó la cita
    Pendiente = 1,          // sin conexión: la cita se guardó localmente para sincronizar luego
    SlotOcupado = 2,        // el servidor o SQLite detectaron conflicto de horario
    Error = 3,              // error inesperado (red, servidor, etc.)
    PendienteSinConexion = 4,
    HoraOcupada = 5,
    Invalida = 6
}

/// <summary>
/// Encapsula el resultado de BookingService.ReservarAsync().
/// Siempre incluye un Mensaje descriptivo para mostrar en la UI.
/// </summary>
public class ResultadoReserva
{
    /// <summary>Clasificación del resultado.</summary>
    public ResultadoReservaTipo Tipo { get; set; }

    /// <summary>Cita creada (disponible cuando Tipo = Confirmada o Pendiente).</summary>
    public Cita? Cita { get; set; }

    /// <summary>Mensaje legible para mostrar al usuario en la pantalla de confirmación.</summary>
    public string? Mensaje { get; set; }
}
