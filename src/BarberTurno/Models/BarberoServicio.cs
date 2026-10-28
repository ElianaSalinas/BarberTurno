// Responsable: M02
// Tabla puente N:M entre Barbero y Servicio. Se persiste en SQLite.
using SQLite;

namespace BarberTurno.Models;

/// <summary>
/// Registro de qué servicios ofrece cada barbero.
/// La unicidad de la combinación (BarberoId, ServicioId) la garantiza M03
/// mediante un índice único en la base de datos.
/// </summary>
[Table("barbero_servicios")]
public class BarberoServicio
{
    /// <summary>Clave primaria autoincremental generada por SQLite.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>FK hacia la tabla de barberos. Indexado para consultas por barbero.</summary>
    [Indexed]
    public int BarberoId { get; set; }

    /// <summary>FK hacia la tabla de servicios. Indexado para consultas por servicio.</summary>
    [Indexed]
    public int ServicioId { get; set; }
}
