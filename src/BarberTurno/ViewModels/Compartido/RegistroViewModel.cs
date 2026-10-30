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

/// <summary>Valida y registra una cuenta antes de dirigirla al área correspondiente a su rol.</summary>
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
        MensajeError = string.Empty;

        // ── Validación 1: campos obligatorios ─────────────────────────────
        if (string.IsNullOrWhiteSpace(Nombre)    ||
            string.IsNullOrWhiteSpace(Telefono)  ||
            string.IsNullOrWhiteSpace(Usuario)   ||
            string.IsNullOrWhiteSpace(Password)  ||
            string.IsNullOrWhiteSpace(ConfirmarPassword))
        {
            MensajeError = "Completa todos los campos.";
            return;
        }

        // ── Validación 2: longitud mínima del nombre ──────────────────────
        if (Nombre.Trim().Length < 3)
        {
            MensajeError = "El nombre debe tener al menos 3 caracteres.";
            return;
        }

        // ── Validación 3: teléfono de 10 dígitos (RD) ────────────────────
        // Quitamos espacios y guiones primero (el usuario puede escribir "809-555-1234").
        string telefonoLimpio = Telefono.Replace(" ", "").Replace("-", "");
        if (telefonoLimpio.Length != 10 || !telefonoLimpio.All(char.IsDigit))
        {
            MensajeError = "Ingresa un teléfono válido de 10 dígitos.";
            return;
        }

        // ── Validación 4: usuario sin espacios y mínimo 3 caracteres ─────
        string usuarioTrim = Usuario.Trim();
        if (usuarioTrim.Length < 3 || usuarioTrim.Contains(' '))
        {
            MensajeError = "El usuario debe tener al menos 3 caracteres y sin espacios.";
            return;
        }

        // ── Validación 5: contraseña mínimo 6 caracteres ─────────────────
        if (Password.Length < 6)
        {
            MensajeError = "La contraseña debe tener al menos 6 caracteres.";
            return;
        }

        // ── Validación 6: confirmación de contraseña ──────────────────────
        if (Password != ConfirmarPassword)
        {
            MensajeError = "Las contraseñas no coinciden.";
            return;
        }

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
