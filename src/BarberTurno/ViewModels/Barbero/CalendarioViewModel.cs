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
            var resultado = await AgendaLoader.CargarAsync(
                _auth.SesionActual, DateOnly.FromDateTime(FechaSeleccionada), DateTime.Now,
                _citas, _clientes, _servicios);

            Citas.Clear();

            if (resultado is null)
            {
                // Sesión sin BarberoId: no se consulta nada (nunca se usa otro Id).
                SinCitas = true;
                Resumen = "No se encontró tu perfil de barbero. Cierra sesión e inicia de nuevo.";
                return;
            }

            foreach (var i in resultado.Items) Citas.Add(i);

            SinCitas = Citas.Count == 0;
            Resumen = Citas.Count == 0
                ? "Sin turnos este día"
                : $"{Citas.Count} turno{(Citas.Count == 1 ? "" : "s")}" + (resultado.Ingresos > 0 ? $" · ${resultado.Ingresos:N0}" : "");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}
