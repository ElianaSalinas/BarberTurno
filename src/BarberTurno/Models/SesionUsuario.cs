// Responsable: M02
// Datos de la sesión activa en memoria. NO se guarda en SQLite; el token vive en SecureStorage (M05).
namespace BarberTurno.Models;

/// <summary>
/// Representa la sesión del usuario que está autenticado en este momento.
/// Se crea desde LoginResponseDto en DtoMapper y se mantiene en memoria durante la sesión.
/// </summary>
public class SesionUsuario
{
    /// <summary>Id del Cliente o del Barbero según el rol activo.</summary>
    public int UsuarioId { get; set; }

    /// <summary>Nombre para mostrar en la UI.</summary>
    public string Nombre { get; set; } = "";

    /// <summary>Rol activo: determina qué flujo de Shell se presenta.</summary>
    public RolUsuario Rol { get; set; }

    /// <summary>
    /// JWT devuelto por el servidor. Se almacena en SecureStorage (M05);
    /// aquí se guarda solo en memoria para adjuntarlo a las peticiones HTTP.
    /// </summary>
    public string Token { get; set; } = "";
}
