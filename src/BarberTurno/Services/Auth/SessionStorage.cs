// Responsable: M06
// Implementa ISessionStorage con SecureStorage de MAUI.
// NUNCA se guarda la contraseña. Solo token, expiración y datos de sesión.
using System.Text.Json;
using BarberTurno.Models;

namespace BarberTurno.Services.Auth;

/// <summary>Guarda de forma segura el token, vencimiento y perfil usados para restaurar la sesión.</summary>
public class SessionStorage : ISessionStorage
{
    // ── Claves de SecureStorage ────────────────────────────────────────────────
    // Se definen como constantes privadas para evitar cadenas mágicas dispersas.
    // Si algún día hay que renombrarlas, se cambia en un solo lugar.
    private const string ClaveToken   = "auth_token";
    private const string ClaveExpira  = "auth_expira";
    private const string ClaveSesion  = "auth_sesion";

    // ── Guardar ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <summary>Persiste los datos de sesión después de un acceso o registro exitoso.</summary>
    public async Task SaveSesionAsync(string token, DateTime expiraEnUtc, SesionUsuario sesion)
    {
        // Guardamos el token tal cual llega del servidor.
        await SecureStorage.Default.SetAsync(ClaveToken, token);

        // Convertimos la fecha a ISO 8601 "round-trip" ("o") para no perder
        // la zona horaria. Ejemplo: "2025-11-01T03:00:00.0000000Z"
        await SecureStorage.Default.SetAsync(ClaveExpira, expiraEnUtc.ToString("o"));

        // Serializamos solo UsuarioId, Nombre y Rol (nunca la contraseña).
        string json = JsonSerializer.Serialize(sesion);
        await SecureStorage.Default.SetAsync(ClaveSesion, json);
    }

    // ── Leer token ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(ClaveToken);
        }
        catch
        {
            // En Android, si la app se reinstala o el Keystore se invalida,
            // SecureStorage lanza una excepción. Limpiamos nuestras claves
            // y devolvemos null para que la app muestre el login en vez de caer.
            Clear();
            return null;
        }
    }

    // ── Leer expiración ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<DateTime?> GetExpiracionAsync()
    {
        try
        {
            string? raw = await SecureStorage.Default.GetAsync(ClaveExpira);

            // Si nunca se guardó nada, raw es null → devolvemos null.
            if (raw is null) return null;

            // Intentamos parsear el ISO 8601. Si el string está dañado
            // (truncado, corrupto) o no tiene zona horaria, ParseExact lanza.
            // RoundtripKind garantiza que el DateTime resultante sea UTC.
            if (DateTime.TryParseExact(
                    raw,
                    "o",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out DateTime expira))
            {
                return expira;
            }

            // El dato estaba dañado: lo limpiamos todo y avisamos con null.
            Clear();
            return null;
        }
        catch
        {
            Clear();
            return null;
        }
    }

    // ── Leer datos de sesión ──────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<SesionUsuario?> GetSesionAsync()
    {
        try
        {
            string? json = await SecureStorage.Default.GetAsync(ClaveSesion);

            if (json is null) return null;

            // Intentamos deserializar. Si el JSON está truncado o corrupto,
            // JsonSerializer lanza JsonException.
            return JsonSerializer.Deserialize<SesionUsuario>(json);
        }
        catch
        {
            // Dato corrupto o Keystore invalidado: limpiamos y devolvemos null.
            Clear();
            return null;
        }
    }

    // ── Limpiar ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <summary>Elimina los datos protegidos al cerrar sesión o detectar que expiró.</summary>
    public void Clear()
    {
        // Eliminamos SOLO nuestras tres claves.
        // NUNCA usamos SecureStorage.Default.RemoveAll() porque borraría
        // datos de otros módulos u otras librerías de la misma app.
        SecureStorage.Default.Remove(ClaveToken);
        SecureStorage.Default.Remove(ClaveExpira);
        SecureStorage.Default.Remove(ClaveSesion);
    }
}
