// Responsable: M02
// Enumeraciones compartidas del dominio.
namespace BarberTurno.Models;

public enum RolUsuario { Cliente, Barbero }

/// <summary>Ciclo de vida de una cita.</summary>
public enum EstadoCita
{
    PendienteConfirmacion, // creada sin conexión o aún no confirmada por el servidor
    Confirmada,
    Rechazada,             // el servidor la rechazó (p. ej. hora ocupada al sincronizar)
    Cancelada,
    Atendida,              // opcional
    Ausente                // opcional
}

