using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Navigation;

namespace BarberTurno.ViewModels;

/// <summary>Prepara la pantalla inicial del cliente y sus accesos a barberos y servicios.</summary>
public partial class InicioViewModel : BaseViewModel
{
    private static readonly CultureInfo Es = new("es-ES");

    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private readonly IBarberoRepository _barberos;
    private readonly IServicioRepository _servicios;
    private readonly ICitaRepository _citas;
    private bool _cargando;

    public InicioViewModel(IAuthService auth, INavigationService nav, IBarberoRepository barberos,
                           IServicioRepository servicios, ICitaRepository citas)
    {
        _auth = auth; _nav = nav; _barberos = barberos; _servicios = servicios; _citas = citas;
    }

    [ObservableProperty] private string saludo = "Hola.";
    [ObservableProperty] private string fecha = "";

    // Próximo turno
    [ObservableProperty] private bool tieneProxima;
    [ObservableProperty] private string proximaFecha = "";
    [ObservableProperty] private string proximaHora = "";
    [ObservableProperty] private string proximaServicio = "";
    [ObservableProperty] private string proximaBarbero = "";
    [ObservableProperty] private string proximaBarberoInicial = "";
    [ObservableProperty] private string diaNumero = "";
    [ObservableProperty] private string mesCorto = "";

    public ObservableCollection<BarberoItem> Barberos { get; } = new();

    public override void OnAppearing()
    {
        var sesion = _auth.SesionActual;
        var primero = (sesion?.Nombre ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        Saludo = string.IsNullOrEmpty(primero) ? "Hola." : $"Hola, {primero}.";
        Fecha = DateTime.Now.ToString("dddd, d 'de' MMMM", Es).ToUpperInvariant();
        _ = CargarAsync(sesion);
    }

    [RelayCommand]
    private Task ReintentarAsync() => CargarAsync(_auth.SesionActual);

    private async Task CargarAsync(SesionUsuario? sesion)
    {
        if (_cargando) return;
        _cargando = true;
        var fallo = false;
        State = ViewState.Cargando;
        MensajeError = null;

        try
        {
            var lista = (await _barberos.GetAllAsync()).OrderBy(b => b.Nombre).Take(6).ToList();
            var nuevosBarberos = new List<BarberoItem>();

            foreach (var b in lista)
            {
                var servs = await _servicios.GetByBarberoAsync(b.Id);
                nuevosBarberos.Add(new BarberoItem(b.Id, b.Nombre, b.FotoUrl, string.Join(" · ", servs.Select(s => s.Nombre))));
            }

            Barberos.Clear();
            foreach (var item in nuevosBarberos)
            {
                Barberos.Add(item);
            }

            TieneProxima = false;
            if (sesion is null) return;

            var ahora = DateTime.Now;
            var proxima = (await _citas.GetProximasClienteAsync(sesion.UsuarioId, ahora))
                .Where(c => c.Estado is EstadoCita.Confirmada or EstadoCita.Pendiente)
                .OrderBy(c => c.Inicio).FirstOrDefault();
            if (proxima is null) return;

            var barbero = await _barberos.GetByIdAsync(proxima.BarberoId);
            var servicio = await _servicios.GetByIdAsync(proxima.ServicioId);
            ProximaFecha = char.ToUpper(proxima.Inicio.ToString("dddd, d 'de' MMMM", Es)[0]) + proxima.Inicio.ToString("dddd, d 'de' MMMM", Es)[1..];
            ProximaHora = proxima.Inicio.ToString("h:mm tt", CultureInfo.InvariantCulture);
            ProximaServicio = servicio?.Nombre ?? "";
            ProximaBarbero = barbero?.Nombre ?? "";
            ProximaBarberoInicial = string.IsNullOrEmpty(ProximaBarbero) ? "" : ProximaBarbero[..1].ToUpperInvariant();
            DiaNumero = proxima.Inicio.Day.ToString("00");
            MesCorto = proxima.Inicio.ToString("MMM", Es).ToUpperInvariant().TrimEnd('.');
            TieneProxima = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            fallo = true;
            MensajeError = "No se pudieron cargar tus datos. Revisa tu conexión e inténtalo de nuevo.";
            State = ViewState.Error;
        }
        finally
        {
            _cargando = false;
            if (!fallo)
                State = ViewState.ConDatos;
        }
    }

    [RelayCommand] private Task ReservarAsync() => _nav.GoToAsync("//cliente/barberos");
    [RelayCommand] private Task MisTurnosAsync() => _nav.GoToAsync("//cliente/miscitas");

    [RelayCommand]
    private Task SeleccionarAsync(BarberoItem? barbero) =>
        barbero is null ? Task.CompletedTask
            : _nav.GoToAsync(Routes.Servicios, new Dictionary<string, object> { [NavParams.BarberoId] = barbero.Id });
}
