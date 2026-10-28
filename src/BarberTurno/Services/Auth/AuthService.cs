// Responsable: M05
// Implementa IAuthService: login contra la API, restaurar sesión desde
// SecureStorage y logout. Nunca guarda la contraseña.
using BarberTurno.Dtos;
using BarberTurno.Models;
using BarberTurno.Services.Api;

namespace BarberTurno.Services.Auth;

public class AuthService : IAuthService
{
    // ── Dependencias inyectadas ───────────────────────────────────────────────
    // El constructor recibe las interfaces, nunca las clases concretas.
    // Así el contenedor de DI puede cambiar la implementación sin tocar aquí.
    private readonly IBarberTurnoApi  _api;
    private readonly ISessionStorage  _sessionStorage;

    // ── Estado en memoria ─────────────────────────────────────────────────────
    // SesionActual es null hasta que LoginAsync o RestaurarSesionAsync tengan éxito.
    // Es público porque lo leen PerfilViewModel, ReservasViewModel, etc.
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
            // 1. Construimos el DTO con las credenciales (nunca se guardan).
            //    LoginRequestDto vive en Dtos/Dtos.cs (módulo M05 de red).
            var request = new LoginRequestDto
            {
                Usuario  = usuario,
                Password = password   // Solo viaja en la petición HTTP, jamás se persiste.
            };

            // 2. Llamamos a la API. ApiResult<T> es el sobre tipado de M05:
            //    evita que tengamos que atrapar HttpRequestException aquí.
            var resultado = await _api.LoginAsync(request);

            // 3. ¿Sin conexión? El módulo de red pone SinConexion = true
            //    cuando hay un timeout o fallo de red.
            if (resultado.SinConexion)
                return ResultadoLogin.SinConexion;

            // 4. ¿Credenciales incorrectas? El servidor devuelve HTTP 401.
            if (resultado.CodigoHttp == 401)
                return ResultadoLogin.CredencialesInvalidas;

            // 5. ¿Éxito con datos? Validamos que el sobre contenga datos reales.
            if (resultado.Exito && resultado.Datos is { } datos)
            {
                // 5a. Convertimos el rol string -> enum.
                //     ignoreCase: true acepta "cliente", "Cliente", "CLIENTE".
                //     El rol lo dicta SIEMPRE el servidor, nunca la UI.
                if (!Enum.TryParse<RolUsuario>(datos.Rol, ignoreCase: true, out var rol))
                {
                    // Si el servidor devuelve un rol desconocido (bug de API o
                    // versión desactualizada), no guardamos nada y reportamos error.
                    return ResultadoLogin.Error;
                }

                // 5b. Construimos el objeto de sesión (sin contraseña).
                var sesion = DtoASesion(datos, rol);

                // 5c. Persistimos en SecureStorage: token, expiración y datos de sesión.
                await _sessionStorage.SaveSesionAsync(datos.Token, datos.ExpiraEnUtc, sesion);

                // 5d. Actualizamos el estado en memoria.
                SesionActual = sesion;

                return ResultadoLogin.Exito;
            }

            // 6. La respuesta vino sin error pero sin datos útiles: caso raro,
            //    puede ser un bug del servidor o un contrato roto.
            return ResultadoLogin.Error;
        }
        catch
        {
            // 7. Excepción completamente inesperada (p. ej. NullReferenceException
            //    en alguna capa). No dejamos caer la app: reportamos Error.
            return ResultadoLogin.Error;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // RESTAURAR SESIÓN
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<bool> RestaurarSesionAsync()
    {
        // Leemos las tres piezas. Cualquiera puede devolver null si:
        //   - Nunca se guardó (primera vez que abre la app).
        //   - El Keystore de Android fue invalidado (reinstalación).
        //   - El dato estaba dañado (SessionStorage ya hizo Clear() en ese caso).
        var token  = await _sessionStorage.GetTokenAsync();
        var expira = await _sessionStorage.GetExpiracionAsync();
        var sesion = await _sessionStorage.GetSesionAsync();

        // Si falta cualquiera de las tres, la sesión no es utilizable.
        if (token is null || expira is null || sesion is null)
        {
            _sessionStorage.Clear(); // Limpieza defensiva por si quedó basura parcial.
            SesionActual = null;
            return false;
        }

        // Comprobamos si el token todavía es válido.
        // AddMinutes(1): margen de 60 segundos para evitar que la app deje pasar
        // un token que expirará antes de que el servidor lo procese.
        // (Un token "válido" con 10 segundos de vida causaría un error 401 en la
        //  siguiente petición, con peor experiencia de usuario.)
        if (expira.Value <= DateTime.UtcNow.AddMinutes(1))
        {
            _sessionStorage.Clear();
            SesionActual = null;
            return false;
        }

        // Todo en orden: reconstruimos la sesión en memoria.
        // NO llamamos a la API: funciona sin conexión a internet.
        SesionActual = sesion;
        return true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LOGOUT
    // ═════════════════════════════════════════════════════════════════════════
    public Task LogoutAsync()
    {
        // Borramos el token y los datos de SecureStorage.
        _sessionStorage.Clear();

        // Limpiamos la sesión en memoria.
        SesionActual = null;

        // Task.CompletedTask porque Clear() es síncrono.
        // Si en el futuro Clear() se vuelve async, basta cambiar esta línea.
        return Task.CompletedTask;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // MÉTODO PRIVADO: conversión DTO -> modelo de dominio
    // ═════════════════════════════════════════════════════════════════════════
    // NOTA: Lo ideal sería mover esta lógica a DtoMapper (Mappers/DtoMapper.cs),
    // que es el lugar arquitectónico para conversiones DTO<->Modelo.
    // No lo hacemos aquí porque DtoMapper no es un archivo del módulo M05
    // y no podemos modificarlo sin coordinarlo con el responsable.
    private static SesionUsuario DtoASesion(LoginResponseDto dto, RolUsuario rol)
        => new()
        {
            UsuarioId = dto.UsuarioId,
            Nombre    = dto.Nombre,
            Rol       = rol    // Viene de Enum.TryParse, nunca de la UI.
        };
}
