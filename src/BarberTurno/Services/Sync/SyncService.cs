// Responsable: M07
// Implementa ISyncService; reintenta al volver la conexión con protección contra excepciones y duplicidad.
using System.Diagnostics;
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
    private readonly ICitaRepository _citaRepo;
    private readonly IBarberTurnoApi _api;
    private readonly SemaphoreSlim _catalogLock = new(1, 1);
    private readonly SemaphoreSlim _pendingLock = new(1, 1);

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
                try
                {
                    await SincronizarCatalogosAsync();
                    await SincronizarPendientesAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SyncService] Excepción capturada en auto-sincronización: {ex}");
                }
            }
        };
    }

    public async Task SincronizarCatalogosAsync()
    {
        if (!_networkService.HayConexion) return;

        // Esperar el semáforo sin descartar: si BarberosViewModel o ServiciosViewModel llaman cuando SQLite está vacío,
        // deben esperar a que la sincronización en curso termine antes de leer para no obtener listas vacías falsas.
        await _catalogLock.WaitAsync();

        try
        {
            // 1. Sincronizar servicios PRIMERO para que existan antes de vincularlos con barberos
            var resultadoServicios = await _api.GetServiciosAsync();
            if (resultadoServicios.Exito && resultadoServicios.Datos != null)
            {
                foreach (var servicioDto in resultadoServicios.Datos)
                {
                    await _servicioRepo.SaveItemAsync(servicioDto.ToEntidad());
                }
            }

            // 2. Sincronizar barberos CON sus relaciones de servicios
            var resultadoBarberos = await _api.GetBarberosAsync();
            if (resultadoBarberos.Exito && resultadoBarberos.Datos != null)
            {
                foreach (var barberoDto in resultadoBarberos.Datos)
                {
                    var barbero = barberoDto.ToEntidad();

                    // Distinct evita violar el índice único IX_BarberoServicio_Unico (BarberoId, ServicioId) si la API repite un ID
                    var relaciones = (barberoDto.ServicioIds ?? new List<int>())
                        .Distinct()
                        .Select(sId => new BarberoServicio { BarberoId = barbero.Id, ServicioId = sId });

                    await _barberoRepo.UpsertAsync(barbero, relaciones);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SyncService] Error al sincronizar catálogos: {ex.Message}");
        }
        finally
        {
            _catalogLock.Release();
        }
    }

    public async Task SincronizarPendientesAsync()
    {
        if (!_networkService.HayConexion) return;

        if (!await _pendingLock.WaitAsync(0))
        {
            Debug.WriteLine("[SyncService] Sincronización de pendientes ya en progreso, omitiendo.");
            return;
        }

        try
        {
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
                    Debug.WriteLine($"[SyncService] Error al sincronizar cita {cita.Id}: {ex.Message}");
                }
            }
        }
        finally
        {
            _pendingLock.Release();
        }
    }
}
