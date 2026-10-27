using BarberTurno.Models;
// Responsable: M06
// Inicio/cierre/restauración de sesión y rol activo.
namespace BarberTurno.Services.Auth;

public interface IAuthService
{
    SesionUsuario? SesionActual { get; }
    Task<bool> LoginAsync(string usuario, string password);
    Task<bool> RestaurarSesionAsync();   // al abrir la app: token válido -> entra directo según el rol
    Task LogoutAsync();                  // borra token y limpia datos sensibles
}

