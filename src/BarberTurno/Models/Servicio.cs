// Responsable: M02
// Entidad local Servicio. Se persiste en SQLite; se sincroniza desde la API vía DtoMapper.
using SQLite;

namespace BarberTurno.Models;

/// <summary>
/// Servicio ofrecido en la barbería (corte, barba, etc.).
/// La tabla se crea en M03 (AppDatabase).
/// </summary>
[Table("servicios")]
public class Servicio
{
    /// <summary>
    /// Clave primaria asignada por el servidor. NO lleva AutoIncrement para mantener
    /// el mismo Id de la API y que ServicioId en Cita siempre apunte al servicio correcto.
    /// </summary>
    [PrimaryKey]
    public int Id { get; set; }

    /// <summary>Nombre del servicio. Indexado para búsquedas rápidas.</summary>
    [Indexed]
    public string Nombre { get; set; } = "";

    /// <summary>Duración estimada del servicio en minutos. Usada por M07 para calcular Fin.</summary>
    public int DuracionMin { get; set; }

    /// <summary>Precio del servicio.</summary>
    public decimal Precio { get; set; }

    /// <summary>Foto del resultado del servicio (URL o imagen embebida data:image). Opcional.</summary>
    public string? FotoUrl { get; set; }
}
