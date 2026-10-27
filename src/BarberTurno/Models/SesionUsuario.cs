// Responsable: M06
// Datos de la sesión activa en memoria (el token vive en SecureStorage).
namespace BarberTurno.Models;

public class SesionUsuario
{
    public int UsuarioId { get; set; }       // Id de Cliente o de Barbero según el rol
    public string Nombre { get; set; } = "";
    public RolUsuario Rol { get; set; }
}

