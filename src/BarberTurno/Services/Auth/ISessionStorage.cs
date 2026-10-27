// Responsable: M06
// Envoltorio de SecureStorage: SOLO el token, nunca la contraseña.
namespace BarberTurno.Services.Auth;

public interface ISessionStorage
{
    Task SaveTokenAsync(string token);
    Task<string?> GetTokenAsync();
    void Clear();
}

