// Responsable: M06
// Envoltorio de SecureStorage: SOLO el token, nunca la contraseña.
using BarberTurno.Models;

namespace BarberTurno.Services.Auth;

public interface ISessionStorage
{
    /// <summary>
    /// Guarda el token JWT, su fecha de expiración y los datos de sesión
    /// en una sola operación atómica-conceptual.
    /// </summary>
    Task SaveSesionAsync(string token, DateTime expiraEnUtc, SesionUsuario sesion);

    /// <summary>
    /// Devuelve el token guardado, o null si no existe o hay error.
    /// Lo necesita el módulo de red para adjuntarlo en cada petición.
    /// </summary>
    Task<string?> GetTokenAsync();

    /// <summary>
    /// Devuelve la fecha de expiración del token, o null si no existe o está dañada.
    /// </summary>
    Task<DateTime?> GetExpiracionAsync();

    /// <summary>
    /// Devuelve los datos de sesión (sin contraseña), o null si no existen o están dañados.
    /// Permite restaurar la sesión aunque no haya conexión a internet.
    /// </summary>
    Task<SesionUsuario?> GetSesionAsync();

    /// <summary>
    /// Elimina SOLO las claves de este módulo. Nunca llama a RemoveAll().
    /// </summary>
    void Clear();
}
