// Responsable: M5
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
    /// Registra una cuenta nueva como Cliente o Barbero.
    /// Si el registro tiene éxito, la sesión queda iniciada automáticamente
    /// (mismo comportamiento que LoginAsync exitoso).
    /// NUNCA guarda la contraseña.
    /// </summary>
    Task<ResultadoRegistro> RegistrarAsync(
        string nombre,
        string telefono,
        string usuario,
        string password,
        RolUsuario rol);

    /// <summary>
    /// Al abrir la app: si hay token válido en SecureStorage y no expiró,
    /// reconstruye SesionActual y devuelve true (sin llamar a la API).
    /// Lo llama LoginViewModel.InicializarAsync() desde OnAppearing.
    /// </summary>
    Task<bool> RestaurarSesionAsync();

    /// <summary>
    /// Borra token y datos de sesión. Deja SesionActual = null.
    /// </summary>
    Task LogoutAsync();
}
