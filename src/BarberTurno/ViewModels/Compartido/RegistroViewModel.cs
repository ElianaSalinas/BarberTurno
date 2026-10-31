// Responsable: M5
// Valida campos de registro, llama a IAuthService.RegistrarAsync,
// traduce el resultado en mensajes y navega según el rol del servidor.
// Sin lógica de negocio: eso es responsabilidad de AuthService.
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Navigation;

namespace BarberTurno.ViewModels;

public partial class RegistroViewModel : BaseViewModel
{
    // ── Dependencias ─────────────────────────────────────────────────────────
    private readonly IAuthService       _authService;
    private readonly INavigationService _nav;

    // ── Propiedades observables ───────────────────────────────────────────────

    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _usuario = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmarPassword = string.Empty;

    [ObservableProperty]
    private string _nombreError = string.Empty;

    [ObservableProperty]
    private string _telefonoError = string.Empty;

    [ObservableProperty]
    private string _usuarioError = string.Empty;

    [ObservableProperty]
    private string _passwordError = string.Empty;

    [ObservableProperty]
    private string _confirmarPasswordError = string.Empty;

    private bool _haIntentadoRegistrar;

    // TODO(M5): borrar MensajeError cuando M1 lo agregue en BaseViewModel.
    [ObservableProperty]
    private string _mensajeError = string.Empty;

    // TODO(M5): borrar IsBusy cuando M1 lo agregue en BaseViewModel.
    [ObservableProperty]
    private bool _isBusy;

    // ── Selección de rol ──────────────────────────────────────────────────────
    // EsCliente y EsBarbero controlan los RadioButton de la vista.
    // RolSeleccionado siempre refleja cuál de los dos está activo.

    [ObservableProperty]
    private bool _esCliente = true;   // Cliente seleccionado por defecto.

    [ObservableProperty]
    private bool _esBarbero;

    // Propiedad de dominio que siempre está sincronizada con los RadioButton.
    // No es [ObservableProperty] porque la vista no la enlaza directamente:
    // el ViewModel la usa internamente al llamar a RegistrarAsync.
    public RolUsuario RolSeleccionado { get; private set; } = RolUsuario.Cliente;

    // ── Métodos parciales generados por [ObservableProperty] ─────────────────
    // [ObservableProperty] declara "partial void OnXChanged(T value)" para que
    // nosotros podamos agregar lógica extra cuando la propiedad cambia.

    partial void OnEsClienteChanged(bool value)
    {
        // Solo reacciona cuando el usuario ACTIVA "Cliente".
        // Cuando value=false (lo desactivamos nosotros desde OnEsBarberoChanged),
        // no hacemos nada para evitar un ciclo infinito.
        if (!value) return;
        EsBarbero        = false;
        RolSeleccionado  = RolUsuario.Cliente;
    }

    partial void OnEsBarberoChanged(bool value)
    {
        // Igual: solo reacciona cuando se activa "Barbero".
        if (!value) return;
        EsCliente       = false;
        RolSeleccionado = RolUsuario.Barbero;
    }

    partial void OnNombreChanged(string value)
    {
        // En tiempo real: caracteres no permitidos
        if (!Validaciones.EsSoloLetras(value))
        {
            NombreError = "Solo se permiten letras y espacios.";
            return;
        }

        // Si ya intentó registrarse, revalidar longitud y obligatoriedad en tiempo real
        if (_haIntentadoRegistrar)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                NombreError = "El nombre es obligatorio.";
                return;
            }
            if (value.Trim().Length < 3)
            {
                NombreError = "El nombre debe tener al menos 3 caracteres.";
                return;
            }
        }

        NombreError = string.Empty;
    }

    partial void OnTelefonoChanged(string value)
    {
        // En tiempo real: solo números, guiones y espacios
        if (!Validaciones.EsTelefonoCaracteresValidos(value))
        {
            TelefonoError = "Solo se permiten números.";
            return;
        }

        // Si ya intentó registrarse, revalidar 10 dígitos y obligatoriedad en tiempo real
        if (_haIntentadoRegistrar)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                TelefonoError = "El teléfono es obligatorio.";
                return;
            }
            if (!Validaciones.EsTelefonoValido(value))
            {
                TelefonoError = "El teléfono debe tener exactamente 10 dígitos.";
                return;
            }
        }

        TelefonoError = string.Empty;
    }

    partial void OnUsuarioChanged(string value)
    {
        // En tiempo real: caracteres no permitidos y sin espacios
        if (!Validaciones.EsUsuarioCaracteresValidos(value, permiteArroba: false))
        {
            UsuarioError = "Solo letras, números, punto, guion y guion bajo (sin espacios).";
            return;
        }

        // Si ya intentó registrarse, revalidar longitud y obligatoriedad en tiempo real
        if (_haIntentadoRegistrar)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                UsuarioError = "El usuario es obligatorio.";
                return;
            }
            if (value.Trim().Length < 3)
            {
                UsuarioError = "El usuario debe tener al menos 3 caracteres.";
                return;
            }
        }

        UsuarioError = string.Empty;
    }

    partial void OnPasswordChanged(string value)
    {
        if (_haIntentadoRegistrar)
        {
            if (string.IsNullOrEmpty(value))
            {
                PasswordError = "La contraseña es obligatoria.";
            }
            else if (value.Length < 6)
            {
                PasswordError = "La contraseña debe tener al menos 6 caracteres.";
            }
            else
            {
                PasswordError = string.Empty;
            }
        }
        else
        {
            PasswordError = string.Empty;
        }

        // Revalidar coincidencia con ConfirmarPassword si ya tiene suficiente longitud
        if (!string.IsNullOrEmpty(ConfirmarPassword) && ConfirmarPassword.Length >= value.Length)
        {
            ConfirmarPasswordError = ConfirmarPassword == value ? string.Empty : "Las contraseñas no coinciden.";
        }
    }

    partial void OnConfirmarPasswordChanged(string value)
    {
        // Mostrar "no coinciden" solo cuando tenga al menos la misma longitud que Password
        if (!string.IsNullOrEmpty(Password) && value.Length >= Password.Length)
        {
            ConfirmarPasswordError = value == Password ? string.Empty : "Las contraseñas no coinciden.";
            return;
        }

        if (_haIntentadoRegistrar)
        {
            if (string.IsNullOrEmpty(value))
            {
                ConfirmarPasswordError = "Confirma tu contraseña.";
                return;
            }
            if (value != Password)
            {
                ConfirmarPasswordError = "Las contraseñas no coinciden.";
                return;
            }
        }

        ConfirmarPasswordError = string.Empty;
    }

    // ── Constructor ───────────────────────────────────────────────────────────
    public RegistroViewModel(IAuthService authService, INavigationService nav)
    {
        _authService = authService;
        _nav         = nav;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // COMANDO: REGISTRAR
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task RegistrarAsync()
    {
        // Protección contra doble toque.
        if (IsBusy) return;

        _haIntentadoRegistrar = true;
        MensajeError = string.Empty;

        bool hayError = false;

        // ── Validación 1: Nombre ──────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            NombreError = "El nombre es obligatorio.";
            hayError = true;
        }
        else if (!Validaciones.EsSoloLetras(Nombre))
        {
            NombreError = "Solo se permiten letras y espacios.";
            hayError = true;
        }
        else if (Nombre.Trim().Length < 3)
        {
            NombreError = "El nombre debe tener al menos 3 caracteres.";
            hayError = true;
        }
        else
        {
            NombreError = string.Empty;
        }

        // ── Validación 2: Teléfono de 10 dígitos (RD) ────────────────────
        if (string.IsNullOrWhiteSpace(Telefono))
        {
            TelefonoError = "El teléfono es obligatorio.";
            hayError = true;
        }
        else if (!Validaciones.EsTelefonoCaracteresValidos(Telefono))
        {
            TelefonoError = "Solo se permiten números.";
            hayError = true;
        }
        else if (!Validaciones.EsTelefonoValido(Telefono))
        {
            TelefonoError = "El teléfono debe tener exactamente 10 dígitos.";
            hayError = true;
        }
        else
        {
            TelefonoError = string.Empty;
        }

        // ── Validación 3: Usuario sin espacios y mínimo 3 caracteres ─────
        if (string.IsNullOrWhiteSpace(Usuario))
        {
            UsuarioError = "El usuario es obligatorio.";
            hayError = true;
        }
        else if (!Validaciones.EsUsuarioCaracteresValidos(Usuario, permiteArroba: false))
        {
            UsuarioError = "Solo letras, números, punto, guion y guion bajo (sin espacios).";
            hayError = true;
        }
        else if (Usuario.Trim().Length < 3)
        {
            UsuarioError = "El usuario debe tener al menos 3 caracteres.";
            hayError = true;
        }
        else
        {
            UsuarioError = string.Empty;
        }

        // ── Validación 4: Contraseña mínimo 6 caracteres ─────────────────
        if (string.IsNullOrEmpty(Password))
        {
            PasswordError = "La contraseña es obligatoria.";
            hayError = true;
        }
        else if (Password.Length < 6)
        {
            PasswordError = "La contraseña debe tener al menos 6 caracteres.";
            hayError = true;
        }
        else
        {
            PasswordError = string.Empty;
        }

        // ── Validación 5: Confirmación de contraseña ──────────────────────
        if (string.IsNullOrEmpty(ConfirmarPassword))
        {
            ConfirmarPasswordError = "Confirma tu contraseña.";
            hayError = true;
        }
        else if (ConfirmarPassword != Password)
        {
            ConfirmarPasswordError = "Las contraseñas no coinciden.";
            hayError = true;
        }
        else
        {
            ConfirmarPasswordError = string.Empty;
        }

        if (hayError)
            return;

        string telefonoLimpio = Validaciones.LimpiarTelefono(Telefono);
        string usuarioTrim = Usuario.Trim();

        // ── Llamada al servicio ────────────────────────────────────────────
        // telefonoLimpio y usuarioTrim declarados arriba son accesibles aquí
        // porque son variables locales del mismo método.
        try
        {
            IsBusy = true;

            var resultado = await _authService.RegistrarAsync(
                Nombre.Trim(),
                telefonoLimpio,   // ya sin espacios ni guiones
                usuarioTrim,      // ya sin espacios laterales
                Password,         // solo viaja en la petición HTTP, nunca se guarda
                RolSeleccionado); // el que el usuario eligió en la UI

            switch (resultado)
            {
                case ResultadoRegistro.Exito:
                    // Navegamos con el rol que DEVOLVIÓ EL SERVIDOR, no RolSeleccionado.
                    await NavegarSegunRolAsync();
                    break;

                case ResultadoRegistro.UsuarioYaExiste:
                    MensajeError = "Ese usuario ya está registrado. Elige otro.";
                    break;

                case ResultadoRegistro.DatosInvalidos:
                    MensajeError = "Los datos no son válidos. Revísalos e inténtalo de nuevo.";
                    break;

                case ResultadoRegistro.SinConexion:
                    MensajeError = "No hay conexión a internet. Revisa tu red e inténtalo de nuevo.";
                    break;

                case ResultadoRegistro.Error:
                default:
                    MensajeError = "Ocurrió un error. Inténtalo más tarde.";
                    break;
            }
        }
        finally
        {
            // Siempre: liberar el indicador y borrar contraseñas de memoria.
            IsBusy              = false;
            Password            = string.Empty;
            ConfirmarPassword   = string.Empty;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // COMANDO: VOLVER AL LOGIN
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task VolverALoginAsync()
    {
        // GoBackAsync saca RegistroPage de la pila y regresa a LoginPage.
        await _nav.GoBackAsync();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // HELPER PRIVADO: navegación según el rol que devolvió el servidor
    // ═════════════════════════════════════════════════════════════════════════
    private async Task NavegarSegunRolAsync()
    {
        // Si SesionActual es null (no debería ocurrir tras un registro exitoso),
        // no navegamos para no romper la app.
        if (_authService.SesionActual is null) return;

        // El rol viene del servidor (SesionActual.Rol), no de RolSeleccionado.
        var ruta = _authService.SesionActual.Rol == RolUsuario.Barbero
            ? Routes.Barbero   // "//barbero"
            : Routes.Cliente;  // "//cliente"

        // "//" = ruta absoluta: reemplaza la pila completa (login + registro desaparecen).
        await _nav.GoToAsync(ruta);
    }
}
