// Responsable: M5
// Implementa IAuthService: login contra la API, registro, restaurar sesión
// desde SecureStorage y logout. Nunca guarda la contraseña.
using BarberTurno.Dtos;
using BarberTurno.Models;
using BarberTurno.Services.Api;

namespace BarberTurno.Services.Auth;

/// <summary>Gestiona el acceso y la sesión activa conectando la API con el almacenamiento seguro del dispositivo.</summary>
public class AuthService : IAuthService
{
    // ── Dependencias inyectadas ───────────────────────────────────────────────
    private readonly IBarberTurnoApi _api;
    private readonly ISessionStorage _sessionStorage;

    // ── Estado en memoria ─────────────────────────────────────────────────────
    public SesionUsuario? SesionActual { get; private set; }

    // ── Constructor ───────────────────────────────────────────────────────────
    /// <summary>Recibe el cliente REST y el almacén seguro que conserva la sesión.</summary>
    public AuthService(IBarberTurnoApi api, ISessionStorage sessionStorage)
    {
        _api            = api;
        _sessionStorage = sessionStorage;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LOGIN
    // ═════════════════════════════════════════════════════════════════════════
    /// <summary>Valida las credenciales en la API y guarda la sesión y JWT al tener éxito.</summary>
    public async Task<ResultadoLogin> LoginAsync(string usuario, string password)
    {
        try
        {
            var request = new LoginRequestDto
            {
                Usuario  = usuario,
                Password = password
            };

            var resultado = await _api.LoginAsync(request);

            if (resultado.SinConexion)
                return ResultadoLogin.SinConexion;

            if (resultado.CodigoHttp == 401)
                return ResultadoLogin.CredencialesInvalidas;

            if (resultado.Exito && resultado.Datos is { } datos)
            {
                if (!Enum.TryParse<RolUsuario>(datos.Rol, ignoreCase: true, out var rol))
                    return ResultadoLogin.Error;

                var sesion = DtoASesion(datos, rol);
                await _sessionStorage.SaveSesionAsync(datos.Token, datos.ExpiraEnUtc, sesion);
                SesionActual = sesion;
                _api.SetAuthToken(datos.Token);
                return ResultadoLogin.Exito;
            }

            return ResultadoLogin.Error;
        }
        catch
        {
            return ResultadoLogin.Error;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // REGISTRO
    // ═════════════════════════════════════════════════════════════════════════
    /// <summary>Registra el usuario remoto e inicia su sesión inmediatamente.</summary>
    public async Task<ResultadoRegistro> RegistrarAsync(
        string nombre,
        string telefono,
        string usuario,
        string password,
        RolUsuario rol)
    {
        try
        {
            var request = new RegistroRequestDto
            {
                Nombre   = nombre,
                Telefono = telefono,
                Usuario  = usuario,
                Password = password,
                Rol      = rol.ToString()
            };

            var resultado = await _api.RegistrarAsync(request);

            if (resultado.SinConexion)
                return ResultadoRegistro.SinConexion;

            if (resultado.CodigoHttp == 409)
                return ResultadoRegistro.UsuarioYaExiste;

            if (resultado.CodigoHttp == 400)
                return ResultadoRegistro.DatosInvalidos;

            if (resultado.Exito && resultado.Datos is { } datos)
            {
                if (!Enum.TryParse<RolUsuario>(datos.Rol, ignoreCase: true, out var rolServidor))
                    return ResultadoRegistro.Error;

                var sesion = DtoASesion(datos, rolServidor);
                await _sessionStorage.SaveSesionAsync(datos.Token, datos.ExpiraEnUtc, sesion);
                SesionActual = sesion;
                _api.SetAuthToken(datos.Token);
                return ResultadoRegistro.Exito;
            }

            return ResultadoRegistro.Error;
        }
        catch
        {
            return ResultadoRegistro.Error;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // RESTAURAR SESIÓN
    // ═════════════════════════════════════════════════════════════════════════
    /// <summary>Restaura una sesión vigente desde SecureStorage y vuelve a configurar el JWT.</summary>
    public async Task<bool> RestaurarSesionAsync()
    {
        var token  = await _sessionStorage.GetTokenAsync();
        var expira = await _sessionStorage.GetExpiracionAsync();
        var sesion = await _sessionStorage.GetSesionAsync();

        if (token is null || expira is null || sesion is null)
        {
            _sessionStorage.Clear();
            SesionActual = null;
            return false;
        }

        // Sesiones guardadas antes de existir BarberoId: sin ese dato la agenda quedaría vacía; se pide un nuevo login.
        if (sesion.Rol == RolUsuario.Barbero && sesion.BarberoId is not > 0)
        {
            _sessionStorage.Clear();
            SesionActual = null;
            return false;
        }

        if (expira.Value <= DateTime.UtcNow.AddMinutes(1))
        {
            _sessionStorage.Clear();
            SesionActual = null;
            return false;
        }

        SesionActual = sesion;
        _api.SetAuthToken(token);
        return true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LOGOUT
    // ═════════════════════════════════════════════════════════════════════════
    /// <summary>Elimina la sesión local y evita que el token se envíe en futuras solicitudes.</summary>
    public Task LogoutAsync()
    {
        _sessionStorage.Clear();
        SesionActual = null;
        _api.SetAuthToken(null);
        return Task.CompletedTask;
    }

    private static SesionUsuario DtoASesion(LoginResponseDto dto, RolUsuario rol)
        => new()
        {
            UsuarioId = dto.UsuarioId,
            Nombre    = dto.Nombre,
            Rol       = rol,
            BarberoId = dto.BarberoId
        };
}
