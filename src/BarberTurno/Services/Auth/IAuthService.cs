// Responsable: M05
// Inicio/cierre/restauración de sesión y rol activo.
using BarberTurno.Models;

namespace BarberTurno.Services.Auth;

public interface IAuthService
{
    /// <summary>
    /// Sesión activa en memoria. Null si no hay sesión.
    /// La leen los módulos de Perfil, Reservas, etc.
    /// </summary>
    SesionUsuario? SesionActual { get; }

    /// <summary>
    /// Intenta hacer login con las credenciales proporcionadas.
    /// Devuelve un ResultadoLogin para que el ViewModel muestre
    /// el mensaje adecuado en cada caso.
    /// NUNCA guarda la contraseña.
    /// </summary>
    Task<ResultadoLogin> LoginAsync(string usuario, string password);

    /// <summary>
    /// Al abrir la app: si hay token válido en SecureStorage y no expiró,
    /// reconstruye SesionActual y devuelve true (sin llamar a la API).
    /// Si algo falla o expiró, devuelve false y el ViewModel navega al login.
    /// </summary>
    Task<bool> RestaurarSesionAsync();

    /// <summary>
    /// Borra token y datos de sesión. Deja SesionActual = null.
    /// </summary>
    Task LogoutAsync();
}
