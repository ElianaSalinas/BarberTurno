// Responsable: M02
// Enumeraciones compartidas del dominio. Consumidas por M03, M04, M06, M09, M10.
namespace BarberTurno.Models;

/// <summary>
/// Rol del usuario autenticado. Determina qué flujo de Shell se muestra.
/// </summary>
public enum RolUsuario { Cliente, Barbero }

/// <summary>
/// Ciclo de vida completo de una cita. El valor textual equivalente (minúsculas)
/// es el que viaja en el JSON de la API; la conversión la hace DtoMapper.
/// </summary>
public enum EstadoCita
{
    Pendiente,   // creada sin conexión o aún no confirmada por el servidor
    Confirmada,
    Rechazada,   // el servidor la rechazó (p. ej. hora ocupada al sincronizar)
    Cancelada,
    Atendida,    // el barbero marcó la cita como atendida
    Ausente      // el cliente no se presentó
}
