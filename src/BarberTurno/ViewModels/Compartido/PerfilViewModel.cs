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

    /// <summary>La página lo llama desde OnAppearing (solo delega).</summary>
    public void OnAppearing() => Cargar();

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
        var app = Application.Current;
        var tema = app?.UserAppTheme ?? AppTheme.Unspecified;
        if (tema == AppTheme.Unspecified)
            tema = app?.RequestedTheme ?? AppTheme.Unspecified;
        TemaOscuro = tema == AppTheme.Dark;
        _cargandoTema = false;

        MensajeError = null;
        State = ViewState.ConDatos;
    }

    partial void OnTemaOscuroChanged(bool value)
    {
        if (_cargandoTema)
            return;

        try
        {
            if (Application.Current is { } app)
                app.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;

            Preferences.Default.Set(PrefTemaOscuro, value);
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
