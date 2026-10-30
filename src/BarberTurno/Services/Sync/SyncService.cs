// Responsable: M07 / M04
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
    private readonly IClienteRepository _clienteRepo;
    private readonly ICitaRepository _citaRepo;
    private readonly IBarberTurnoApi _api;
    private readonly SemaphoreSlim _catalogLock = new(1, 1);
    private readonly SemaphoreSlim _citasLock = new(1, 1);
    private readonly SemaphoreSlim _pendingLock = new(1, 1);

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
                try
                {
                    await SincronizarCatalogosAsync();
                    await SincronizarCitasAsync();
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
            try
            {
                var resultadoServicios = await _api.GetServiciosAsync();
                if (resultadoServicios.Exito && resultadoServicios.Datos != null)
                {
                    var servicios = resultadoServicios.Datos.Select(s => s.ToEntidad()).ToList();
                    await _servicioRepo.ReplaceAllAsync(servicios);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SyncService] Error al sincronizar servicios: {ex.Message}");
            }

            // 2. Sincronizar barberos CON sus relaciones de servicios de forma atómica
            try
            {
                var resultadoBarberos = await _api.GetBarberosAsync();
                if (resultadoBarberos.Exito && resultadoBarberos.Datos != null)
                {
                    var barberos = new List<Barbero>();
                    var todasRelaciones = new List<BarberoServicio>();

                    foreach (var barberoDto in resultadoBarberos.Datos)
                    {
                        var barbero = barberoDto.ToEntidad();
                        barberos.Add(barbero);

                        var relaciones = (barberoDto.ServicioIds ?? new List<int>())
                            .Distinct()
                            .Select(sId => new BarberoServicio { BarberoId = barbero.Id, ServicioId = sId });

                        todasRelaciones.AddRange(relaciones);
                    }

                    await _barberoRepo.ReplaceAllAsync(barberos, todasRelaciones);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SyncService] Error al sincronizar barberos: {ex.Message}");
            }

            // 3. Sincronizar clientes
            try
            {
                var resultadoClientes = await _api.GetClientesAsync();
                if (resultadoClientes.Exito && resultadoClientes.Datos != null)
                {
                    var clientes = resultadoClientes.Datos.Select(c => c.ToEntidad()).ToList();
                    await _clienteRepo.UpsertRangeAsync(clientes);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SyncService] Error al sincronizar clientes: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SyncService] Error general al sincronizar catálogos: {ex.Message}");
        }
        finally
        {
            _catalogLock.Release();
        }
    }

    public async Task SincronizarCitasAsync()
    {
        if (!_networkService.HayConexion) return;

        if (!await _citasLock.WaitAsync(0))
        {
            Debug.WriteLine("[SyncService] Sincronización de citas ya en progreso, omitiendo.");
            return;
        }

        try
        {
            var resultadoCitas = await _api.GetMisCitasAsync();
            if (resultadoCitas.Exito && resultadoCitas.Datos != null)
            {
                foreach (var citaDto in resultadoCitas.Datos)
                {
                    try
                    {
                        await _citaRepo.SaveItemAsync(citaDto.ToEntidad());
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SyncService] Error al guardar cita {citaDto.Id}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SyncService] Error al sincronizar citas: {ex.Message}");
        }
        finally
        {
            _citasLock.Release();
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
