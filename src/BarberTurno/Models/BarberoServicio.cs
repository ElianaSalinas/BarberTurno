// Responsable: M02
// Tabla puente N:M entre Barbero y Servicio. Se persiste en SQLite.
using SQLite;

namespace BarberTurno.Models;

/// <summary>
/// Registro de qué servicios ofrece cada barbero.
/// Es una tabla puente local con Id autoincremental, pero protege contra duplicados
/// mediante el índice único compuesto IX_BarberoServicio_Unico sobre (BarberoId, ServicioId).
/// </summary>
[Table("barbero_servicios")]
/// <summary>Relaciona un barbero con un servicio que puede ofrecer.</summary>
public class BarberoServicio
{
    /// <summary>Clave primaria autoincremental local generada por SQLite.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// FK hacia la tabla de barberos. Forma parte del índice compuesto único
    /// para impedir que se duplique el par (BarberoId, ServicioId).
    /// </summary>
    [Indexed(Name = "IX_BarberoServicio_Unico", Order = 1, Unique = true)]
    public int BarberoId { get; set; }

    /// <summary>
    /// FK hacia la tabla de servicios. Segunda columna del índice compuesto único
    /// sobre (BarberoId, ServicioId).
    /// </summary>
    [Indexed(Name = "IX_BarberoServicio_Unico", Order = 2, Unique = true)]
    public int ServicioId { get; set; }
}
