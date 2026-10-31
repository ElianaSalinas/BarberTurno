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

    public string Detalle => $"{FormatoCliente.Precio(Precio)} · {DuracionMin} min";
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

    [ObservableProperty]
    private string nuevoServicioNombreError = "";

    [ObservableProperty]
    private string nuevoServicioPrecioError = "";

    [ObservableProperty]
    private string nuevoServicioDuracionError = "";

    [ObservableProperty]
    private string mensajeErrorServicio = "";

    private bool _haIntentadoCrearServicio;

    partial void OnNuevoServicioNombreChanged(string value)
    {
        if (!Validaciones.EsNombreServicioCaracteresValidos(value))
        {
            NuevoServicioNombreError = "Solo se permiten letras, números y signos comunes (+, -).";
            return;
        }

        if (_haIntentadoCrearServicio)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                NuevoServicioNombreError = "El nombre del servicio es obligatorio.";
                return;
            }
            if (value.Trim().Length < 3)
            {
                NuevoServicioNombreError = "El nombre del servicio debe tener al menos 3 caracteres.";
                return;
            }
        }

        NuevoServicioNombreError = "";
    }

    partial void OnNuevoServicioPrecioChanged(string value)
    {
        if (!Validaciones.EsPrecioCaracteresValidos(value))
        {
            NuevoServicioPrecioError = "Solo se permiten números y un punto decimal.";
            return;
        }

        if (_haIntentadoCrearServicio)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                NuevoServicioPrecioError = "El precio es obligatorio.";
                return;
            }
            if (!Validaciones.EsPrecioValido(value, out _))
            {
                NuevoServicioPrecioError = "Ingresa un precio válido mayor a 0 (ej. 450.50).";
                return;
            }
        }

        NuevoServicioPrecioError = "";
    }

    partial void OnNuevoServicioDuracionChanged(string value)
    {
        if (!Validaciones.EsEnteroCaracteresValidos(value))
        {
            NuevoServicioDuracionError = "Solo se permiten números enteros.";
            return;
        }

        if (_haIntentadoCrearServicio)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                NuevoServicioDuracionError = "La duración es obligatoria.";
                return;
            }
            if (!Validaciones.EsDuracionValida(value, out _))
            {
                NuevoServicioDuracionError = "Ingresa una duración válida entre 5 y 480 minutos.";
                return;
            }
        }

        NuevoServicioDuracionError = "";
    }

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
            await CargarDatosBarberoAsync(sesion.BarberoId);
        }

        MensajeError = null;
        State = ViewState.ConDatos;
    }

    private async Task CargarDatosBarberoAsync(int? barberoId)
    {
        try
        {
            // El barbero se identifica por BarberoId de la sesión (no por nombre).
            if (barberoId is not > 0)
            {
                _barberoId = 0;
                Debug.WriteLine("[PerfilViewModel] La sesión no tiene BarberoId.");
                return;
            }

            var barbero = await _barberos.GetByIdAsync(barberoId.Value);

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
        _haIntentadoCrearServicio = true;
        bool hayError = false;

        if (string.IsNullOrWhiteSpace(NuevoServicioNombre))
        {
            NuevoServicioNombreError = "El nombre del servicio es obligatorio.";
            hayError = true;
        }
        else if (!Validaciones.EsNombreServicioCaracteresValidos(NuevoServicioNombre))
        {
            NuevoServicioNombreError = "Solo se permiten letras, números y signos comunes (+, -).";
            hayError = true;
        }
        else if (NuevoServicioNombre.Trim().Length < 3)
        {
            NuevoServicioNombreError = "El nombre del servicio debe tener al menos 3 caracteres.";
            hayError = true;
        }
        else
        {
            NuevoServicioNombreError = "";
        }

        decimal precio = 0;
        if (string.IsNullOrWhiteSpace(NuevoServicioPrecio))
        {
            NuevoServicioPrecioError = "El precio es obligatorio.";
            hayError = true;
        }
        else if (!Validaciones.EsPrecioCaracteresValidos(NuevoServicioPrecio))
        {
            NuevoServicioPrecioError = "Solo se permiten números y un punto decimal.";
            hayError = true;
        }
        else if (!Validaciones.EsPrecioValido(NuevoServicioPrecio, out precio))
        {
            NuevoServicioPrecioError = "Ingresa un precio válido mayor a 0 (ej. 450.50).";
            hayError = true;
        }
        else
        {
            NuevoServicioPrecioError = "";
        }

        int duracion = 0;
        if (string.IsNullOrWhiteSpace(NuevoServicioDuracion))
        {
            NuevoServicioDuracionError = "La duración es obligatoria.";
            hayError = true;
        }
        else if (!Validaciones.EsEnteroCaracteresValidos(NuevoServicioDuracion))
        {
            NuevoServicioDuracionError = "Solo se permiten números enteros.";
            hayError = true;
        }
        else if (!Validaciones.EsDuracionValida(NuevoServicioDuracion, out duracion))
        {
            NuevoServicioDuracionError = "Ingresa una duración válida entre 5 y 480 minutos.";
            hayError = true;
        }
        else
        {
            NuevoServicioDuracionError = "";
        }

        if (hayError)
            return;

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
                NuevoServicioDuracion = "30";
                NuevoServicioNombreError = "";
                NuevoServicioPrecioError = "";
                NuevoServicioDuracionError = "";
                _haIntentadoCrearServicio = false;

                var sesion = _auth.SesionActual;
                if (sesion != null)
                    await CargarDatosBarberoAsync(sesion.BarberoId);

                await _dialogs.ShowAlertAsync("Servicio creado", "El nuevo servicio ha sido registrado y asignado a tu perfil.");
            }
            else
            {
                MensajeErrorServicio = "No se pudo registrar el nuevo servicio en el servidor.";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            MensajeErrorServicio = "Ocurrió un problema al crear el servicio.";
        }
    }

    /// <summary>
    /// Valida y analiza de manera robusta precios ingresados por el usuario.
    /// Soporta formatos con separador de miles y decimales ("1,500", "1.500", "1,500.50", "1.500,50", "500").
    /// </summary>
    public static bool TryParsePrecio(string? raw, out decimal precio)
    {
        precio = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var texto = raw.Trim();

        // 1. Si contiene tanto punto como coma: el último separador es el decimal y el anterior es miles
        if (texto.Contains('.') && texto.Contains(','))
        {
            int idxPunto = texto.LastIndexOf('.');
            int idxComa = texto.LastIndexOf(',');
            if (idxPunto > idxComa)
            {
                texto = texto.Replace(",", "");
            }
            else
            {
                texto = texto.Replace(".", "").Replace(',', '.');
            }
        }
        // 2. Si solo contiene un separador (',' o '.') seguido de exactamente 3 dígitos al final (ej. 1,500 o 1.500), es separador de miles
        else if (System.Text.RegularExpressions.Regex.IsMatch(texto, @"^\d{1,3}[,\.]\d{3}$"))
        {
            texto = texto.Replace(",", "").Replace(".", "");
        }
        else
        {
            texto = texto.Replace(',', '.');
        }

        return decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out precio) && precio > 0;
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
