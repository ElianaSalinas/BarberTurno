// Responsable: M02
// Entidad local Cita. TODO(M02): índice único filtrado (BarberoId, Inicio) solo para estados activos.
namespace BarberTurno.Models;

public class Cita
{
    /// <summary>Se genera en el dispositivo (Guid). También sirve de clave de idempotencia ante el servidor.</summary>
    public Guid Id { get; set; }
    public int ClienteId { get; set; }
    public int BarberoId { get; set; }
    public int ServicioId { get; set; }
    public DateTime Inicio { get; set; }   // hora local; el traductor convierte desde/hacia UTC
    public DateTime Fin { get; set; }      // Inicio + duración del servicio
    public EstadoCita Estado { get; set; }
    public string? MotivoRechazo { get; set; }
}

