// Responsable: M5
// Valida campos, llama a IAuthService, traduce el resultado en un mensaje
// y navega según el rol. Sin lógica de negocio: eso es responsabilidad de AuthService.
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Navigation;

namespace BarberTurno.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    // ── Constante de ruta ─────────────────────────────────────────────────────
    // TODO(M5): cambiar por Routes.Registro cuando M1 agregue esa constante.
    private const string RutaRegistro = "registro";

    // ── Dependencias ─────────────────────────────────────────────────────────
    // Solo IAuthService e INavigationService: IDialogService no está implementado
    // ni registrado en DI todavía, inyectarlo haría que la app se cierre al abrir.
    private readonly IAuthService      _authService;
    private readonly INavigationService _nav;

    // ── Propiedades observables ───────────────────────────────────────────────
    // [ObservableProperty] genera la propiedad pública con NotifyPropertyChanged.
    // El campo privado se llama con guion bajo + minúscula; la propiedad pública,
    // en PascalCase (usuario → Usuario, password → Password, etc.).

    [ObservableProperty]
    private string _usuario = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    // TODO(M5): borrar IsBusy cuando M1 lo agregue en BaseViewModel.
    // Se declara aquí porque BaseViewModel todavía tiene ese TODO pendiente.
    [ObservableProperty]
    private bool _isBusy;

    // ── Constructor ───────────────────────────────────────────────────────────
    public LoginViewModel(IAuthService authService, INavigationService nav)
    {
        _authService = authService;
        _nav         = nav;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // INICIALIZAR (llamado desde OnAppearing de LoginPage)
    // ═════════════════════════════════════════════════════════════════════════
    public async Task InicializarAsync()
    {
        try
        {
            // Intenta restaurar sesión desde SecureStorage sin llamar a la API.
            // Si el token es válido, navega directo a la pantalla del rol.
            bool sesionActiva = await _authService.RestaurarSesionAsync();
            if (sesionActiva)
                await NavegarSegunRolAsync();
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
    // [RelayCommand] genera LoginCommand (IAsyncRelayCommand) que:
    //   - Expone CanExecute automáticamente vinculado al parámetro canExecute.
    //   - Se puede enlazar con Command="{Binding LoginCommand}" en XAML.
    [RelayCommand]
    private async Task LoginAsync()
    {
        // Evita el doble clic: si ya está cargando, ignoramos la llamada.
        if (IsBusy) return;

        // Limpiamos el error del intento anterior.
        MensajeError = string.Empty;

        // Validación local: no molestamos al servidor con campos vacíos.
        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            MensajeError = "Ingresa tu usuario y contraseña.";
            return;
        }

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
        // Ruta relativa (sin "//") → se apila sobre el login. M1 debe registrar
        // la ruta "registro" en AppShell.xaml.cs con Routing.RegisterRoute.
        await _nav.GoToAsync(RutaRegistro);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // HELPER PRIVADO: navegación según rol
    // ═════════════════════════════════════════════════════════════════════════
    // Centralizado para evitar duplicar el switch en LoginAsync e InicializarAsync.
    private async Task NavegarSegunRolAsync()
    {
        // Si la sesión es null (no debería ocurrir, pero por seguridad),
        // no navegamos: la pantalla de login sigue visible.
        if (_authService.SesionActual is null) return;

        // El rol viene SIEMPRE de SesionActual, que AuthService llenó con
        // el valor que devolvió el servidor. La UI nunca decide el rol.
        var ruta = _authService.SesionActual.Rol == RolUsuario.Barbero
            ? Routes.Barbero   // "//barbero"
            : Routes.Cliente;  // "//cliente"

        // "//" = ruta absoluta en Shell: reemplaza toda la pila de navegación.
        // El botón Atrás de Android no puede regresar al login.
        await _nav.GoToAsync(ruta);
    }
}
