using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Navigation;

namespace BarberTurno.ViewModels;

/// <summary>Calendario del barbero: elige un día y muestra sus turnos reales.</summary>
/// <summary>Presenta las citas del barbero en calendario y abre su detalle.</summary>
public partial class CalendarioViewModel : BaseViewModel
{
    private readonly IAuthService _auth;
    private readonly ICitaRepository _citas;
    private readonly IClienteRepository _clientes;
    private readonly IServicioRepository _servicios;
    private readonly INavigationService _nav;

    public ObservableCollection<CitaAgendaItem> Citas { get; } = new();

    [ObservableProperty]
    private DateTime fechaSeleccionada = DateTime.Today;

    [ObservableProperty]
    private string resumen = "";

    [ObservableProperty]
    private bool sinCitas = true;

    public CalendarioViewModel(IAuthService auth, ICitaRepository citas, IClienteRepository clientes, IServicioRepository servicios, INavigationService nav)
    {
        _auth = auth;
        _citas = citas;
        _clientes = clientes;
        _servicios = servicios;
        _nav = nav;
    }

    // Tocar un turno abre su detalle con el historial del cliente.
    [RelayCommand]
    private async Task AbrirCitaAsync(CitaAgendaItem? item)
    {
        if (item is null || item.CitaId == Guid.Empty)
            return;

        await _nav.GoToAsync(Routes.DetalleCita, new Dictionary<string, object> { ["citaId"] = item.CitaId });
    }

    public override void OnAppearing() => _ = CargarAsync();

    partial void OnFechaSeleccionadaChanged(DateTime value) => _ = CargarAsync();

    [RelayCommand]
    private Task ReintentarAsync() => CargarAsync();

    private async Task CargarAsync()
    {
        State = ViewState.Cargando;
        MensajeError = null;

        try
        {
            var resultado = await AgendaLoader.CargarAsync(
                _auth.SesionActual, DateOnly.FromDateTime(FechaSeleccionada), DateTime.Now,
                _citas, _clientes, _servicios);

            Citas.Clear();

            if (resultado is null)
            {
                // Sesión sin BarberoId: no se consulta nada (nunca se usa otro Id).
                SinCitas = true;
                Resumen = string.Empty;
                MensajeError = "No se encontró tu perfil de barbero. Cierra sesión e inicia de nuevo.";
                State = ViewState.Error;
                return;
            }

            foreach (var i in resultado.Items) Citas.Add(i);

            SinCitas = Citas.Count == 0;
            Resumen = Citas.Count == 0
                ? "Sin turnos este día"
                : $"{Citas.Count} turno{(Citas.Count == 1 ? "" : "s")}" + (resultado.Ingresos > 0 ? $" · ${resultado.Ingresos:N0}" : "");
            State = ViewState.ConDatos;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            MensajeError = "No se pudieron cargar los turnos de este día.";
            State = ViewState.Error;
        }
    }
}
