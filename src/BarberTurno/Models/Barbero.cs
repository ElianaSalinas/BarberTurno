// Responsable: M02
// Entidad local Barbero. Se persiste en SQLite; se sincroniza desde la API vía DtoMapper.
using SQLite;

namespace BarberTurno.Models;

/// <summary>
/// Representa a un barbero del establecimiento junto con su horario laboral.
/// La tabla se crea en M03 (AppDatabase) y M07 usa su horario para generar franjas libres sin conexión.
/// </summary>
[Table("barberos")]
/// <summary>Representa al profesional disponible en el catálogo local y en las reservas.</summary>
public class Barbero
{
    /// <summary>
    /// Clave primaria asignada por el servidor. NO lleva AutoIncrement para preservar
    /// el mismo Id del backend al sincronizar catálogos y que BarberoId en Cita apunte al registro correcto.
    /// </summary>
    [PrimaryKey]
    public int Id { get; set; }

    /// <summary>Nombre completo del barbero. Indexado para búsquedas rápidas.</summary>
    [Indexed]
    public string Nombre { get; set; } = "";

    /// <summary>URL de la foto del barbero (viene de la API, puede ser nula).</summary>
    public string? FotoUrl { get; set; }

    /// <summary>
    /// Hora de inicio de la jornada laboral (por defecto 09:00).
    /// SQLite almacena TimeSpan nativamente como ticks; M07 lo usa para abrir las franjas del día.
    /// </summary>
    public TimeSpan HoraEntrada { get; set; } = new TimeSpan(9, 0, 0);

    /// <summary>
    /// Hora de fin de la jornada laboral (por defecto 18:00).
    /// M07 genera franjas hasta que Inicio + DuracionMin no supere HoraSalida.
    /// </summary>
    public TimeSpan HoraSalida { get; set; } = new TimeSpan(18, 0, 0);

    /// <summary>
    /// Días de la semana en que trabaja, separados por comas según DayOfWeek (1=Lunes ... 6=Sábado, 0=Domingo).
    /// Se guarda como texto simple en la misma fila para que SQLite lo persista sin requerir otra tabla puente.
    /// </summary>
    public string DiasTrabajo { get; set; } = "1,2,3,4,5,6";
}
