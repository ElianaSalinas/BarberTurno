using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Dialogs;
using BarberTurno.Services.Navigation;

// Responsable: M09
// Datos del usuario, cambio de tema (claro/oscuro) y cerrar sesión.
namespace BarberTurno.ViewModels;

public partial class PerfilViewModel : BaseViewModel
{
    /// <summary>Clave en Preferences; M10/M01 pueden leerla al iniciar la app para restaurar el tema.</summary>
    public const string PrefTemaOscuro = "tema_oscuro";

    private readonly IAuthService _auth;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private bool _cargandoTema;
    private bool _paginaVisible;

    public PerfilViewModel(IAuthService auth, INavigationService navigation, IDialogService dialogs)
    {
        _auth = auth;
        _navigation = navigation;
        _dialogs = dialogs;
    }

    [ObservableProperty]
    private string nombre = "";

    [ObservableProperty]
    private string rolTexto = "";

    [ObservableProperty]
    private string inicial = "?";

    [ObservableProperty]
    private bool temaOscuro;

    /// <summary>La página avisa al aparecer: marca visible y delega la carga.</summary>
    public override void OnAppearing()
    {
        base.OnAppearing();
        _paginaVisible = true;
        Cargar();
    }

    /// <summary>La página avisa al desaparecer: marca invisible para ignorar reciclaje de controles en Android.</summary>
    public override void OnDisappearing()
    {
        base.OnDisappearing();
        _paginaVisible = false;
    }

    [RelayCommand]
    private void Cargar()
    {
        var sesion = _auth.SesionActual;
        if (sesion is null)
        {
            MensajeError = "No hay una sesión activa. Inicia sesión de nuevo.";
            State = ViewState.Error;
            return;
        }

        Nombre = sesion.Nombre;
        RolTexto = sesion.Rol == RolUsuario.Barbero ? "Barbero" : "Cliente";
        Inicial = string.IsNullOrWhiteSpace(sesion.Nombre) ? "?" : sesion.Nombre.Trim()[..1].ToUpperInvariant();

        // Leer el tema actual sin disparar el cambio de tema.
        _cargandoTema = true;
        try
        {
            var app = Application.Current;
            var tema = app?.UserAppTheme ?? AppTheme.Unspecified;
            if (tema == AppTheme.Unspecified)
                tema = app?.RequestedTheme ?? AppTheme.Unspecified;
            TemaOscuro = tema == AppTheme.Dark;
        }
        finally
        {
            _cargandoTema = false;
        }

        MensajeError = null;
        State = ViewState.ConDatos;
    }

    /// <summary>
    /// Maneja el cambio de estado del Switch. Comparar con el tema actual no basta porque al salir de la pestaña
    /// Android puede reciclar el Switch y reportar un valor invertido; CommunityToolkit solo dispara este método si
    /// el valor difiere del anterior, por lo que la comparación siempre resultaría distinta. La bandera _paginaVisible
    /// asegura que solo se procesen cambios manuales realizados por el usuario mientras la página está activa en pantalla.
    /// </summary>
    partial void OnTemaOscuroChanged(bool value)
    {
        // Ignorar si se está sincronizando inicialmente o si la página no está visible (evento espurio por reciclaje)
        if (_cargandoTema || !_paginaVisible)
            return;

        try
        {
            var app = Application.Current;
            if (app is null)
                return;

            var temaActual = app.UserAppTheme == AppTheme.Unspecified
                ? (app.RequestedTheme == AppTheme.Dark)
                : (app.UserAppTheme == AppTheme.Dark);

            // Si el tema actual ya coincide, evitar reestilizar la app y bloquear la navegación
            if (temaActual == value)
                return;

            Preferences.Default.Set(PrefTemaOscuro, value);

            // Despachar a la cola del hilo principal para no interrumpir el evento del switch ni la transición de Shell
            MainThread.BeginInvokeOnMainThread(() =>
            {
                app.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private async Task CerrarSesionAsync()
    {
        var confirmar = await _dialogs.ConfirmAsync(
            "Cerrar sesión",
            "¿Seguro que quieres cerrar sesión?",
            "Cerrar sesión",
            "Cancelar");

        if (!confirmar)
            return;

        try
        {
            await _auth.LogoutAsync();
            await _navigation.GoToAsync(Routes.Login);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _dialogs.ShowAlertAsync("No se pudo cerrar sesión",
                "Ocurrió un problema. Intenta de nuevo.");
        }
    }
}
