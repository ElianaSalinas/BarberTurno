// Responsable: M07
// Implementa ISyncService; reintenta al volver la conexión.
using BarberTurno.Data.Repositories;
using BarberTurno.Dtos;
using BarberTurno.Mappers;
using BarberTurno.Models;
using BarberTurno.Services.Api;
using BarberTurno.Services.Network;

namespace BarberTurno.Services.Sync;

public class SyncService : ISyncService
{
    private readonly INetworkService _networkService;
    private readonly IBarberoRepository _barberoRepo;
    private readonly IServicioRepository _servicioRepo;
    private readonly IClienteRepository _clienteRepo;
    private readonly ICitaRepository _citaRepo;
    private readonly IBarberTurnoApi _api;

    public event EventHandler<Guid>? ReservaRechazada;

    public SyncService(
        INetworkService networkService,
        IBarberoRepository barberoRepo,
        IServicioRepository servicioRepo,
        IClienteRepository clienteRepo,
        ICitaRepository citaRepo,
        IBarberTurnoApi api)
    {
        _networkService = networkService;
        _barberoRepo = barberoRepo;
        _servicioRepo = servicioRepo;
        _clienteRepo = clienteRepo;
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
            if (resultadoBarberos.Exito && resultadoBarberos.Datos != null)
            {
                foreach (var barberoDto in resultadoBarberos.Datos)
                {
                    var relaciones = (barberoDto.ServicioIds ?? new List<int>())
                        .Select(sid => new BarberoServicio { BarberoId = barberoDto.Id, ServicioId = sid });
                    await _barberoRepo.UpsertAsync(barberoDto.ToEntidad(), relaciones);
                }
            }

            var resultadoServicios = await _api.GetServiciosAsync();
            if (resultadoServicios.Exito && resultadoServicios.Datos != null)
            {
                foreach (var servicioDto in resultadoServicios.Datos)
                {
                    await _servicioRepo.SaveItemAsync(servicioDto.ToEntidad());
                }
            }

            var resultadoClientes = await _api.GetClientesAsync();
            if (resultadoClientes.Exito && resultadoClientes.Datos != null)
            {
                foreach (var clienteDto in resultadoClientes.Datos)
                {
                    await _clienteRepo.UpsertAsync(clienteDto.ToEntidad());
                }
            }

            var resultadoCitas = await _api.GetMisCitasAsync();
            if (resultadoCitas.Exito && resultadoCitas.Datos != null)
            {
                foreach (var citaDto in resultadoCitas.Datos)
                {
                    await _citaRepo.SaveItemAsync(citaDto.ToEntidad());
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
                var request = new CrearCitaRequestDto
                {
                    Id = cita.Id,
                    BarberoId = cita.BarberoId,
                    ServicioId = cita.ServicioId,
                    InicioUtc = cita.Inicio.ToUniversalTime()
                };

                var respuesta = await _api.CrearCitaAsync(request);

                if (respuesta.Exito)
                {
                    cita.Estado = EstadoCita.Confirmada;
                    cita.PendienteSincronizar = false;
                    await _citaRepo.SaveItemAsync(cita);
                }
                else if (respuesta.CodigoHttp == 409)
                {
                    cita.Estado = EstadoCita.Rechazada;
                    cita.PendienteSincronizar = false;
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
