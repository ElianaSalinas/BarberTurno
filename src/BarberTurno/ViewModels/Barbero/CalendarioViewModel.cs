using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Services.Auth;

namespace BarberTurno.ViewModels;

/// <summary>Calendario del barbero: elige un día y muestra sus turnos reales.</summary>
public partial class CalendarioViewModel : BaseViewModel
{
    private readonly IAuthService _auth;
    private readonly ICitaRepository _citas;
    private readonly IClienteRepository _clientes;
    private readonly IServicioRepository _servicios;

    public ObservableCollection<CitaAgendaItem> Citas { get; } = new();

    [ObservableProperty]
    private DateTime fechaSeleccionada = DateTime.Today;

    [ObservableProperty]
    private string resumen = "";

    [ObservableProperty]
    private bool sinCitas = true;

    public CalendarioViewModel(IAuthService auth, ICitaRepository citas, IClienteRepository clientes, IServicioRepository servicios)
    {
        _auth = auth;
        _citas = citas;
        _clientes = clientes;
        _servicios = servicios;
    }

    public override void OnAppearing() => _ = CargarAsync();

    partial void OnFechaSeleccionadaChanged(DateTime value) => _ = CargarAsync();

    private async Task CargarAsync()
    {
        try
        {
            var (items, ingresos) = await AgendaLoader.CargarAsync(
                _auth, _citas, _clientes, _servicios, DateOnly.FromDateTime(FechaSeleccionada));

            Citas.Clear();
            foreach (var i in items) Citas.Add(i);

            SinCitas = Citas.Count == 0;
            Resumen = Citas.Count == 0
                ? "Sin turnos este día"
                : $"{Citas.Count} turno{(Citas.Count == 1 ? "" : "s")}" + (ingresos > 0 ? $" · ${ingresos:N0}" : "");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}
