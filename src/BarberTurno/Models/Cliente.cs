// Responsable: M02
// Entidad local Cliente. Se persiste en SQLite; también se sincroniza desde la API vía DtoMapper.
using SQLite;

namespace BarberTurno.Models;

/// <summary>
/// Representa a un cliente registrado. La tabla se crea en M03 (AppDatabase).
/// </summary>
[Table("clientes")]
public class Cliente
{
    /// <summary>Clave primaria autoincremental generada por SQLite.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Nombre completo del cliente. Indexado para búsquedas rápidas.</summary>
    [Indexed]
    public string Nombre { get; set; } = "";

    /// <summary>Teléfono de contacto.</summary>
    public string Telefono { get; set; } = "";

    /// <summary>Ruta local de la foto de perfil (opcional).</summary>
    public string? FotoRuta { get; set; }
}
