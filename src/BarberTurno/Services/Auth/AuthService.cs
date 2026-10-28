// Responsable: M5
// Implementa IAuthService: login contra la API, registro, restaurar sesión
// desde SecureStorage y logout. Nunca guarda la contraseña.
using BarberTurno.Dtos;
using BarberTurno.Models;
using BarberTurno.Services.Api;

namespace BarberTurno.Services.Auth;

public class AuthService : IAuthService
{
    // ── Dependencias inyectadas ───────────────────────────────────────────────
    private readonly IBarberTurnoApi _api;
    private readonly ISessionStorage _sessionStorage;

    // ── Estado en memoria ─────────────────────────────────────────────────────
    public SesionUsuario? SesionActual { get; private set; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public AuthService(IBarberTurnoApi api, ISessionStorage sessionStorage)
    {
        _api            = api;
        _sessionStorage = sessionStorage;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LOGIN
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<ResultadoLogin> LoginAsync(string usuario, string password)
    {
        try
        {
            // Construimos el DTO con las credenciales. La contraseña solo existe
            // en memoria mientras dura la petición, jamás se persiste.
            var request = new LoginRequestDto
            {
                Usuario  = usuario,
                Password = password
            };

            var resultado = await _api.LoginAsync(request);

            // Sin conexión: timeout o fallo de red.
            if (resultado.SinConexion)
                return ResultadoLogin.SinConexion;

            // HTTP 401 = no autenticado: el servidor reconoce la ruta pero
            // rechaza las credenciales (usuario o contraseña incorrectos).
            if (resultado.CodigoHttp == 401)
                return ResultadoLogin.CredencialesInvalidas;

            if (resultado.Exito && resultado.Datos is { } datos)
            {
                // El rol lo dicta SIEMPRE el servidor. ignoreCase acepta
                // "cliente", "Cliente" y "CLIENTE".
                if (!Enum.TryParse<RolUsuario>(datos.Rol, ignoreCase: true, out var rol))
                    return ResultadoLogin.Error;

                var sesion = DtoASesion(datos, rol);
                await _sessionStorage.SaveSesionAsync(datos.Token, datos.ExpiraEnUtc, sesion);
                SesionActual = sesion;
                return ResultadoLogin.Exito;
            }

            return ResultadoLogin.Error;
        }
        catch
        {
            // Excepción inesperada: la app no debe caer.
            return ResultadoLogin.Error;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // REGISTRO
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<ResultadoRegistro> RegistrarAsync(
        string nombre,
        string telefono,
        string usuario,
        string password,
        RolUsuario rol)
    {
        try
        {
            // TODO(M5 red): descomentar cuando el módulo de red agregue
            // RegistroRequestDto en Dtos/Dtos.cs e IBarberTurnoApi.RegistrarAsync.
            // ────────────────────────────────────────────────────────────────
            // var request = new RegistroRequestDto
            // {
            //     Nombre   = nombre,
            //     Telefono = telefono,
            //     Usuario  = usuario,
            //     Password = password,       // Solo viaja en la petición HTTP, nunca se persiste.
            //     Rol      = rol.ToString()  // "Cliente" o "Barbero" como texto para la API.
            // };
            //
            // var resultado = await _api.RegistrarAsync(request);
            //
            // // Sin conexión.
            // if (resultado.SinConexion)
            //     return ResultadoRegistro.SinConexion;
            //
            // // HTTP 409 = el usuario (email/nombre) ya existe en el servidor.
            // if (resultado.CodigoHttp == 409)
            //     return ResultadoRegistro.UsuarioYaExiste;
            //
            // // HTTP 400 = datos inválidos: campo vacío, formato incorrecto, rol no permitido.
            // if (resultado.CodigoHttp == 400)
            //     return ResultadoRegistro.DatosInvalidos;
            //
            // if (resultado.Exito && resultado.Datos is { } datos)
            // {
            //     // Usamos el rol que devolvió el SERVIDOR, no el que eligió el usuario.
            //     // Ambos deberían coincidir, pero el servidor es la fuente de verdad.
            //     if (!Enum.TryParse<RolUsuario>(datos.Rol, ignoreCase: true, out var rolServidor))
            //         return ResultadoRegistro.Error;
            //
            //     // Reutilizamos DtoASesion: misma conversión que en LoginAsync,
            //     // sin duplicar código.
            //     var sesion = DtoASesion(datos, rolServidor);
            //     await _sessionStorage.SaveSesionAsync(datos.Token, datos.ExpiraEnUtc, sesion);
            //     SesionActual = sesion;
            //     return ResultadoRegistro.Exito;
            // }
            //
            // return ResultadoRegistro.Error;
            // ────────────────────────────────────────────────────────────────

            // Mientras el módulo de red no entregue RegistrarAsync,
            // el método compila y devuelve Error de forma segura.
            // Esto NO afecta el login ni la restauración de sesión.
            _ = nombre; _ = telefono; _ = usuario; _ = password; _ = rol; // Evita CS0168
            return await Task.FromResult(ResultadoRegistro.Error);
        }
        catch
        {
            return ResultadoRegistro.Error;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // RESTAURAR SESIÓN
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<bool> RestaurarSesionAsync()
    {
        var token  = await _sessionStorage.GetTokenAsync();
        var expira = await _sessionStorage.GetExpiracionAsync();
        var sesion = await _sessionStorage.GetSesionAsync();

        // Si falta cualquiera, no hay sesión utilizable.
        if (token is null || expira is null || sesion is null)
        {
            _sessionStorage.Clear();
            SesionActual = null;
            return false;
        }

        // Margen de 1 minuto: descartamos tokens que expiran casi de inmediato
        // para evitar un 401 en la primera petición tras entrar a la app.
        if (expira.Value <= DateTime.UtcNow.AddMinutes(1))
        {
            _sessionStorage.Clear();
            SesionActual = null;
            return false;
        }

        // Todo válido. No llamamos a la API: funciona sin internet.
        SesionActual = sesion;
        return true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LOGOUT
    // ═════════════════════════════════════════════════════════════════════════
    public Task LogoutAsync()
    {
        _sessionStorage.Clear();
        SesionActual = null;
        return Task.CompletedTask;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // HELPER PRIVADO: DTO -> modelo de dominio
    // ═════════════════════════════════════════════════════════════════════════
    // NOTA: Lo ideal sería mover esta conversión a Mappers/DtoMapper.cs,
    // que es el lugar arquitectónico para transformaciones DTO <-> Modelo.
    // No lo hacemos porque DtoMapper.cs no es un archivo del módulo M5.
    // LoginAsync y RegistrarAsync (cuando esté activo) lo reutilizan sin duplicar.
    private static SesionUsuario DtoASesion(LoginResponseDto dto, RolUsuario rol)
        => new()
        {
            UsuarioId = dto.UsuarioId,
            Nombre    = dto.Nombre,
            Rol       = rol   // Siempre viene del servidor vía Enum.TryParse.
        };
}
