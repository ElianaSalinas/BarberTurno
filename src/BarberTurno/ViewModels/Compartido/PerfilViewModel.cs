using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private string nuevoServicioNombre = "";

    [ObservableProperty]
    private string nuevoServicioPrecio = "";

    [ObservableProperty]
    private string nuevoServicioDuracion = "30";

    public override void OnAppearing() => _ = CargarAsync();

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
        var app = Application.Current;
        var tema = app?.UserAppTheme ?? AppTheme.Unspecified;
        if (tema == AppTheme.Unspecified)
            tema = app?.RequestedTheme ?? AppTheme.Unspecified;
        TemaOscuro = tema == AppTheme.Dark;
        _cargandoTema = false;

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
            var results = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Selecciona una foto de perfil"
            });

            var result = results?.FirstOrDefault();
            if (result == null) return;

            FotoUrl = result.FullPath;

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
    private async Task CrearNuevoServicioAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoServicioNombre))
        {
            await _dialogs.ShowAlertAsync("Atención", "Ingresa el nombre del servicio.");
            return;
        }

        if (!decimal.TryParse(NuevoServicioPrecio, out var precio) || precio <= 0)
        {
            await _dialogs.ShowAlertAsync("Atención", "Ingresa un precio válido mayor a 0.");
            return;
        }

        if (!int.TryParse(NuevoServicioDuracion, out var duracion) || duracion <= 0)
        {
            duracion = 30;
        }

        try
        {
            var res = await _api.CrearServicioAsync(new CrearServicioDto
            {
                Nombre = NuevoServicioNombre.Trim(),
                Precio = precio,
                DuracionMin = duracion
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
                NuevoServicioPrecio = "";
                NuevoServicioDuracion = "30";

                var sesion = _auth.SesionActual;
                if (sesion != null)
                    await CargarDatosBarberoAsync(sesion.UsuarioId);

                await _dialogs.ShowAlertAsync("Servicio creado", "El nuevo servicio ha sido registrado y asignado a tu perfil.");
            }
            else
            {
                await _dialogs.ShowAlertAsync("Error", "No se pudo registrar el nuevo servicio en el servidor.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _dialogs.ShowAlertAsync("Error", "Ocurrió un problema al crear el servicio.");
        }
    }

    partial void OnTemaOscuroChanged(bool value)
    {
        if (_cargandoTema) return;

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
