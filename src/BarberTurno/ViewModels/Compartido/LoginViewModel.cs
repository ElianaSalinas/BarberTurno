// Responsable: M5 / M04
// Valida campos, llama a IAuthService, traduce el resultado en un mensaje
// y navega según el rol. Sin lógica de negocio: eso es responsabilidad de AuthService.
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Navigation;
using BarberTurno.Services.Sync;

namespace BarberTurno.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    // ── Constante de ruta ─────────────────────────────────────────────────────
    private const string RutaRegistro = "registro";

    // ── Dependencias ─────────────────────────────────────────────────────────
    private readonly IAuthService _authService;
    private readonly INavigationService _nav;
    private readonly ISyncService? _sync;
    private bool _inicializado;

    // ── Propiedades observables ───────────────────────────────────────────────
    // MensajeError e IsBusy se heredan directamente de BaseViewModel para evitar advertencias CS0108.
    [ObservableProperty]
    private string _usuario = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _usuarioError = string.Empty;

    [ObservableProperty]
    private string _passwordError = string.Empty;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    // TODO(M5): borrar IsBusy cuando M1 lo agregue en BaseViewModel.
    // Se declara aquí porque BaseViewModel todavía tiene ese TODO pendiente.
    [ObservableProperty]
    private bool _isBusy;

    private bool _haIntentadoLogin;

    partial void OnUsuarioChanged(string value)
    {
        // Validación en tiempo real (mientras escribe): solo caracteres no permitidos
        if (!Validaciones.EsUsuarioCaracteresValidos(value, permiteArroba: true))
        {
            UsuarioError = "Sin espacios. Solo letras, números, punto, guion o correo.";
            return;
        }

        // Si ya intentó entrar antes, revalidar obligatoriedad en tiempo real
        if (_haIntentadoLogin)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                UsuarioError = "El usuario es obligatorio.";
                return;
            }
        }

        UsuarioError = string.Empty;
    }

    partial void OnPasswordChanged(string value)
    {
        // En login, la contraseña solo es obligatoria (sin longitud mínima)
        if (_haIntentadoLogin)
        {
            if (string.IsNullOrEmpty(value))
            {
                PasswordError = "La contraseña es obligatoria.";
                return;
            }
        }

        PasswordError = string.Empty;
    }

    // ── Constructor ───────────────────────────────────────────────────────────
    public LoginViewModel(IAuthService authService, INavigationService nav, ISyncService? sync = null)
    {
        _authService = authService;
        _nav         = nav;
        _sync        = sync;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // INICIALIZAR (llamado desde OnAppearing de LoginPage)
    // ═════════════════════════════════════════════════════════════════════════
    public async Task InicializarAsync()
    {
        // Ejecutar una sola vez para no repetir navegaciones al volver a aparecer la página
        if (_inicializado) return;
        _inicializado = true;

        try
        {
            // Intenta restaurar sesión desde SecureStorage sin llamar a la API.
            // Si el token es válido, navega directo a la pantalla del rol.
            bool sesionActiva = await _authService.RestaurarSesionAsync();
            if (sesionActiva)
            {
                DispararSincronizacionSegundoPlano();
                await NavegarSegunRolAsync();
            }
        }
        catch
        {
            // Si algo falla inesperadamente, simplemente mostramos el login.
            // Nunca cerramos la app por un error en la restauración.
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // COMANDO: LOGIN
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task LoginAsync()
    {
        // Evita el doble clic: si ya está cargando, ignoramos la llamada.
        if (IsBusy) return;

        _haIntentadoLogin = true;

        // Limpiamos el error global del intento anterior.
        MensajeError = string.Empty;

        bool hayError = false;

        // Validación 1: usuario
        if (string.IsNullOrWhiteSpace(Usuario))
        {
            UsuarioError = "El usuario es obligatorio.";
            hayError = true;
        }
        else if (!Validaciones.EsUsuarioCaracteresValidos(Usuario, permiteArroba: true))
        {
            UsuarioError = "Sin espacios. Solo letras, números, punto, guion o correo.";
            hayError = true;
        }
        else
        {
            UsuarioError = string.Empty;
        }

        // Validación 2: contraseña
        if (string.IsNullOrEmpty(Password))
        {
            PasswordError = "La contraseña es obligatoria.";
            hayError = true;
        }
        else
        {
            PasswordError = string.Empty;
        }

        if (hayError)
            return;

        try
        {
            IsBusy = true;

            // Usuario.Trim() elimina espacios accidentales al inicio/final.
            // Password NO recibe Trim() porque algunas contraseñas tienen espacios intencionales.
            var resultado = await _authService.LoginAsync(Usuario.Trim(), Password);

            // Traducimos el enum en un mensaje comprensible para el usuario.
            switch (resultado)
            {
                case ResultadoLogin.Exito:
                    DispararSincronizacionSegundoPlano();
                    await NavegarSegunRolAsync();
                    break;

                case ResultadoLogin.CredencialesInvalidas:
                    MensajeError = "Usuario o contraseña incorrectos.";
                    break;

                case ResultadoLogin.SinConexion:
                    MensajeError = "No hay conexión a internet. Revisa tu red e inténtalo de nuevo.";
                    break;

                case ResultadoLogin.Error:
                default:
                    MensajeError = "Ocurrió un error. Inténtalo más tarde.";
                    break;
            }
        }
        finally
        {
            // El finally se ejecuta SIEMPRE: éxito, error o excepción.
            IsBusy = false;

            // La contraseña se borra de memoria para no dejarla en el Entry
            // aunque el login falle. Seguridad básica de datos en memoria.
            Password = string.Empty;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // COMANDO: IR A REGISTRO
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task IrARegistroAsync()
    {
        await _nav.GoToAsync(RutaRegistro);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // HELPER PRIVADO: navegación según rol
    // ═════════════════════════════════════════════════════════════════════════
    private async Task NavegarSegunRolAsync()
    {
        if (_authService.SesionActual is null) return;

        var ruta = _authService.SesionActual.Rol == RolUsuario.Barbero
            ? Routes.Barbero   // "//barbero"
            : Routes.Cliente;  // "//cliente"

        await _nav.GoToAsync(ruta);
    }

    private void DispararSincronizacionSegundoPlano()
    {
        if (_sync is null) return;
        _ = Task.Run(async () =>
        {
            try
            {
                await _sync.SincronizarCatalogosAsync();
                await _sync.SincronizarCitasAsync();
            }
            catch
            {
                // Silencioso: la sincronización de fondo no interrumpe el flujo del usuario
            }
        });
    }
}
