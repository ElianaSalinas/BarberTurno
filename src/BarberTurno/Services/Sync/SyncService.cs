// Responsable: M07
// Implementa ISyncService; reintenta al volver la conexión.
using BarberTurno.Data.Repositories;
using BarberTurno.Services.Api;
using BarberTurno.Services.Network;

namespace BarberTurno.Services.Sync;

public class SyncService : ISyncService
{
    private readonly INetworkService _networkService;
    private readonly IBarberoRepository _barberoRepo;
    private readonly IServicioRepository _servicioRepo;
    private readonly ICitaRepository _citaRepo;
    private readonly IBarberTurnoApi _api;

    public event EventHandler<Guid>? ReservaRechazada;

    public SyncService(
        INetworkService networkService,
        IBarberoRepository barberoRepo,
        IServicioRepository servicioRepo,
        ICitaRepository citaRepo,
        IBarberTurnoApi api)
    {
        _networkService = networkService;
        _barberoRepo = barberoRepo;
        _servicioRepo = servicioRepo;
        _citaRepo = citaRepo;
        _api = api;

        _networkService.ConexionCambio += async (sender, hayRed) =>
        {
            if (hayRed)
            {
                await SincronizarCatalogosAsync();
                await SincronizarPendientesAsync();
            }
        };
    }

    public async Task SincronizarCatalogosAsync()
    {
        if (!_networkService.HayConexion) return;

        try
        {
            var resultadoBarberos = await _api.GetBarberosAsync();
            if (resultadoBarberos.IsSuccess && resultadoBarberos.Data != null)
            {
                foreach (var barbero in resultadoBarberos.Data)
                {
                    await _barberoRepo.SaveItemAsync(barbero);
                }
            }

            var resultadoServicios = await _api.GetServiciosAsync();
            if (resultadoServicios.IsSuccess && resultadoServicios.Data != null)
            {
                foreach (var servicio in resultadoServicios.Data)
                {
                    await _servicioRepo.SaveItemAsync(servicio);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al sincronizar catalogos: {ex.Message}");
        }
    }

    public async Task SincronizarPendientesAsync()
    {
        if (!_networkService.HayConexion) return;

        var citasPendientes = await _citaRepo.GetPendientesDeSincronizarAsync();

        foreach (var cita in citasPendientes)
        {
            try
            {
                var respuesta = await _api.CrearCitaAsync(cita);

                if (respuesta.IsSuccess)
                {
                    cita.Estado = "Confirmada";
                    cita.IsSynced = true;
                    await _citaRepo.SaveItemAsync(cita);
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    cita.Estado = "Rechazada";
                    cita.IsSynced = true;
                    await _citaRepo.SaveItemAsync(cita);

                    ReservaRechazada?.Invoke(this, cita.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al sincronizar cita {cita.Id}: {ex.Message}");
            }
        }
    }
}