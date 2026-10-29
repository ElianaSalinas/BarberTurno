// Responsable: M02
// Hora libre ofrecida al cliente (no se persiste en SQLite).
namespace BarberTurno.Models;

/// <summary>
/// Representa un bloque horario libre disponible para reservar (en memoria, no va a SQLite).
/// </summary>
public class SlotDisponible
{
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
}
