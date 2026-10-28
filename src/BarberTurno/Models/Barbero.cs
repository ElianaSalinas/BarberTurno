// Responsable: M02
// Entidad local Barbero. Se persiste en SQLite; se sincroniza desde la API vía DtoMapper.
using SQLite;

namespace BarberTurno.Models;

/// <summary>
/// Representa a un barbero del establecimiento. La tabla se crea en M03 (AppDatabase).
/// </summary>
[Table("barberos")]
public class Barbero
{
    /// <summary>Clave primaria autoincremental generada por SQLite.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Nombre completo del barbero. Indexado para búsquedas rápidas.</summary>
    [Indexed]
    public string Nombre { get; set; } = "";

    /// <summary>URL de la foto del barbero (viene de la API, puede ser nula).</summary>
    public string? FotoUrl { get; set; }
}
