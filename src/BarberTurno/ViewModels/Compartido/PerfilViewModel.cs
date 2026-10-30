// Responsable: M09
// Datos del usuario, cambio de tema (claro/oscuro), gestión de perfil de barbero y cerrar sesión.
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using BarberTurno.Data.Repositories;
using BarberTurno.Dtos;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Api;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Dialogs;
using BarberTurno.Services.Navigation;
using BarberTurno.Services.Sync;

namespace BarberTurno.ViewModels;

public partial class ServicioSeleccionableItem : ObservableObject
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int DuracionMin { get; set; }

    [ObservableProperty]
    private bool asignado;

    public string Detalle => $"${Precio:F0} · {DuracionMin} min";
}

public partial class PerfilViewModel : BaseViewModel
{
    public const string PrefTemaOscuro = "tema_oscuro";

    private readonly IAuthService _auth;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IBarberTurnoApi _api;
    private readonly IBarberoRepository _barberos;
    private readonly IServicioRepository _servicios;
    private readonly ISyncService _sync;

    private bool _cargandoTema;
    private bool _paginaVisible;
    private int _barberoId;

    public ObservableCollection<ServicioSeleccionableItem> MisServicios { get; } = new();

    public PerfilViewModel(
        IAuthService auth,
        INavigationService navigation,
        IDialogService dialogs,
        IBarberTurnoApi api,
        IBarberoRepository barberos,
        IServicioRepository servicios,
        ISyncService sync)
    {
        _auth = auth;
        _navigation = navigation;
        _dialogs = dialogs;
        _api = api;
        _barberos = barberos;
        _servicios = servicios;
        _sync = sync;
    }

    [ObservableProperty]
    private string nombre = "";

    [ObservableProperty]
    private string rolTexto = "";

    [ObservableProperty]
    private string inicial = "?";

    [ObservableProperty]
    private string? fotoUrl;

    [ObservableProperty]
    private bool esBarbero;

    [ObservableProperty]
    private bool temaOscuro;

    [ObservableProperty]
    private string? nuevoServicioFoto;

    [ObservableProperty]
    private string nuevoServicioNombre = "";

    [ObservableProperty]
    private string nuevoServicioPrecio = "";

    [ObservableProperty]
    private string nuevoServicioDuracion = "30";

    public List<int> DuracionesDisponibles { get; } = new() { 15, 20, 30, 45, 60, 90, 120 };

    [ObservableProperty]
    private int duracionSeleccionada = 30;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneErrorServicio))]
    private string? mensajeErrorServicio;

    public bool TieneErrorServicio => !string.IsNullOrEmpty(MensajeErrorServicio);

    /// <summary>La página avisa al aparecer: marca visible y delega la carga.</summary>
    public override void OnAppearing()
    {
        base.OnAppearing();
        _paginaVisible = true;
        _ = CargarAsync();
    }

    /// <summary>La página avisa al desaparecer: marca invisible para ignorar reciclaje de controles en Android.</summary>
    public override void OnDisappearing()
    {
        base.OnDisappearing();
        _paginaVisible = false;
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        var sesion = _auth.SesionActual;
        if (sesion is null)
        {
            MensajeError = "No hay una sesión activa. Inicia sesión de nuevo.";
            State = ViewState.Error;
            return;
        }

        Nombre = sesion.Nombre;
        EsBarbero = sesion.Rol == RolUsuario.Barbero;
        RolTexto = EsBarbero ? "Barbero" : "Cliente";
        Inicial = string.IsNullOrWhiteSpace(sesion.Nombre) ? "?" : sesion.Nombre.Trim()[..1].ToUpperInvariant();

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

        if (EsBarbero)
        {
            await CargarDatosBarberoAsync(sesion.UsuarioId);
        }

        MensajeError = null;
        State = ViewState.ConDatos;
    }

    private async Task CargarDatosBarberoAsync(int usuarioId)
    {
        try
        {
            var todosBarberos = await _barberos.GetAllAsync();
            var barbero = todosBarberos.FirstOrDefault(b => b.Nombre.Equals(Nombre, StringComparison.OrdinalIgnoreCase))
                ?? todosBarberos.FirstOrDefault(b => b.Nombre.Equals(Nombre, StringComparison.OrdinalIgnoreCase));

            if (barbero != null)
            {
                _barberoId = barbero.Id;
                FotoUrl = barbero.FotoUrl;

                var todosServicios = await _servicios.GetAllAsync();
                var serviciosDelBarbero = await _servicios.GetByBarberoAsync(barbero.Id);
                var idsAsignados = serviciosDelBarbero.Select(s => s.Id).ToHashSet();

                MisServicios.Clear();
                foreach (var serv in todosServicios)
                {
                    MisServicios.Add(new ServicioSeleccionableItem
                    {
                        Id = serv.Id,
                        Nombre = serv.Nombre,
                        Precio = serv.Precio,
                        DuracionMin = serv.DuracionMin,
                        Asignado = idsAsignados.Contains(serv.Id)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PerfilViewModel] Error cargando datos barbero: {ex}");
        }
    }

    [RelayCommand]
    private async Task CambiarFotoAsync()
    {
        try
        {
            // Pide permiso de acceso a fotos (en Android 13+ el selector del sistema no lo necesita,
            // pero en versiones anteriores sí; así el usuario siempre ve la solicitud).
            var permiso = await Permissions.CheckStatusAsync<Permissions.Photos>();
            if (permiso != PermissionStatus.Granted)
                permiso = await Permissions.RequestAsync<Permissions.Photos>();

            if (permiso != PermissionStatus.Granted)
            {
                await _dialogs.ShowAlertAsync("Permiso necesario",
                    "Para cambiar tu foto necesitamos acceso a tus fotos. Puedes activarlo en Ajustes del teléfono.");
                return;
            }

            var results = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Selecciona una foto de perfil"
            });

            var result = results?.FirstOrDefault();
            if (result == null) return;

            // Se reduce y se guarda embebida para que la vean también los clientes en sus dispositivos.
            await using var origen = await result.OpenReadAsync();
            using var imagen = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(origen);
            using var reducida = imagen.Downsize(480f, false);
            var bytes = reducida.AsBytes(Microsoft.Maui.Graphics.ImageFormat.Jpeg, 0.8f);
            FotoUrl = "data:image/jpeg;base64," + Convert.ToBase64String(bytes);

            if (_barberoId > 0)
            {
                var barbero = await _barberos.GetByIdAsync(_barberoId);
                if (barbero != null)
                {
                    barbero.FotoUrl = FotoUrl;
                    await _barberos.UpsertAsync(barbero, []);
                }

                await _api.ActualizarFotoBarberoAsync(_barberoId, FotoUrl);
                await _dialogs.ShowAlertAsync("Foto actualizada", "Tu foto de perfil se ha guardado correctamente.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _dialogs.ShowAlertAsync("Error", "No se pudo actualizar la foto.");
        }
    }

    [RelayCommand]
    private async Task GuardarServiciosAsignadosAsync()
    {
        if (_barberoId <= 0)
        {
            await _dialogs.ShowAlertAsync("Error", "No se encontró el perfil de barbero asociado.");
            return;
        }

        try
        {
            var seleccionados = MisServicios.Where(s => s.Asignado).Select(s => s.Id).ToList();
            var resultado = await _api.ActualizarServiciosBarberoAsync(_barberoId, seleccionados);

            await _sync.SincronizarCatalogosAsync();
            await _dialogs.ShowAlertAsync("Servicios guardados", "Tus servicios activos han sido actualizados.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _dialogs.ShowAlertAsync("Error", "No se pudieron guardar los servicios.");
        }
    }

    [RelayCommand]
    private async Task ElegirFotoServicioAsync()
    {
        try
        {
            var (foto, denegado) = await FotoHelper.ElegirAsync(800);
            if (denegado)
            {
                await _dialogs.ShowAlertAsync("Permiso necesario",
                    "Necesitamos acceso a tus fotos. Puedes activarlo en Ajustes del teléfono.");
                return;
            }
            if (foto is not null)
                NuevoServicioFoto = foto;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _dialogs.ShowAlertAsync("Error", "No se pudo cargar la foto.");
        }
    }

    [RelayCommand]
    private async Task CrearNuevoServicioAsync()
    {
        MensajeErrorServicio = null;

        if (string.IsNullOrWhiteSpace(NuevoServicioNombre))
        {
            MensajeErrorServicio = "Ingresa el nombre del servicio.";
            await _dialogs.ShowAlertAsync("Atención", MensajeErrorServicio);
            return;
        }

        var precioStr = NuevoServicioPrecio?.Trim().Replace(',', '.');
        if (!decimal.TryParse(precioStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var precio) || precio <= 0)
        {
            MensajeErrorServicio = "Ingresa un precio válido mayor a 0 (ej. 500).";
            await _dialogs.ShowAlertAsync("Atención", MensajeErrorServicio);
            return;
        }

        int duracion = DuracionSeleccionada > 0 ? DuracionSeleccionada : 30;

        try
        {
            var res = await _api.CrearServicioAsync(new CrearServicioDto
            {
                Nombre = NuevoServicioNombre.Trim(),
                Precio = precio,
                DuracionMin = duracion,
                FotoUrl = NuevoServicioFoto
            });

            if (res.Exito && res.Datos != null)
            {
                await _sync.SincronizarCatalogosAsync();

                if (_barberoId > 0)
                {
                    var seleccionados = MisServicios.Where(s => s.Asignado).Select(s => s.Id).ToList();
                    seleccionados.Add(res.Datos.Id);
                    await _api.ActualizarServiciosBarberoAsync(_barberoId, seleccionados);
                    await _sync.SincronizarCatalogosAsync();
                }

                NuevoServicioNombre = "";
                NuevoServicioFoto = null;
                NuevoServicioPrecio = "";
                DuracionSeleccionada = 30;
                MensajeErrorServicio = null;

                var sesion = _auth.SesionActual;
                if (sesion != null)
                    await CargarDatosBarberoAsync(sesion.UsuarioId);

                await _dialogs.ShowAlertAsync("Servicio creado", "El nuevo servicio ha sido registrado y asignado a tu perfil.");
            }
            else
            {
                MensajeErrorServicio = "No se pudo registrar el nuevo servicio en el servidor.";
                await _dialogs.ShowAlertAsync("Error", MensajeErrorServicio);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _dialogs.ShowAlertAsync("Error", "Ocurrió un problema al crear el servicio.");
        }
    }

    /// <summary>
    /// Maneja el cambio de estado del Switch. La bandera _paginaVisible asegura que solo se procesen
    /// cambios manuales realizados por el usuario mientras la página está activa en pantalla.
    /// </summary>
    partial void OnTemaOscuroChanged(bool value)
    {
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

        if (!confirmar) return;

        try
        {
            await _auth.LogoutAsync();
            await _navigation.GoToAsync(Routes.Login);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _dialogs.ShowAlertAsync("No se pudo cerrar sesión", "Ocurrió un problema. Intenta de nuevo.");
        }
    }
}
