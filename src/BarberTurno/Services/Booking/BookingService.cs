// Responsable: M08
// Orquesta la reserva: valida, guarda local, envía al servidor, programa recordatorio.
using BarberTurno.Data.Repositories;
using BarberTurno.Dtos;
using BarberTurno.Models;
using BarberTurno.Services.Api;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Notifications;

namespace BarberTurno.Services.Booking;

/// <summary>Coordina la reserva entre la API, la base SQLite y los recordatorios del dispositivo.</summary>
public class BookingService : IBookingService
{
    private readonly IBarberTurnoApi _api;
    private readonly ICitaRepository _citaRepo;
    private readonly IServicioRepository? _servicioRepo;
    private readonly IAuthService? _auth;
    private readonly IReminderService? _reminders;

    /// <summary>Recibe las dependencias usadas para confirmar o dejar pendiente una cita.</summary>
    public BookingService(
        IBarberTurnoApi api,
        ICitaRepository citaRepo,
        IServicioRepository? servicioRepo = null,
        IAuthService? auth = null,
        IReminderService? reminders = null)
    {
        _api = api;
        _citaRepo = citaRepo;
        _servicioRepo = servicioRepo;
        _auth = auth;
        _reminders = reminders;
    }

    /// <summary>Intenta crear la cita remota y conserva una reserva pendiente cuando no hay conexión.</summary>
    public async Task<ResultadoReserva> ReservarAsync(ReservaSolicitud solicitud)
    {
        var citaId = Guid.NewGuid();
        var request = new CrearCitaRequestDto
        {
            Id = citaId,
            BarberoId = solicitud.BarberoId,
            ServicioId = solicitud.ServicioId,
            InicioUtc = solicitud.Inicio.ToUniversalTime()
        };

        // La API es la autoridad final para impedir reservas simultáneas.
        var apiResult = await _api.CrearCitaAsync(request);

        if (apiResult.Exito && apiResult.Datos != null)
        {
            var cita = new Cita
            {
                Id = apiResult.Datos.Id,
                ClienteId = apiResult.Datos.ClienteId,
                BarberoId = apiResult.Datos.BarberoId,
                ServicioId = apiResult.Datos.ServicioId,
                Inicio = apiResult.Datos.InicioUtc.ToLocalTime(),
                Fin = apiResult.Datos.FinUtc.ToLocalTime(),
                Estado = EstadoCita.Confirmada
            };
            await _citaRepo.InsertOrUpdateAsync(cita);
            await ProgramarRecordatorioAsync(cita);
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Confirmada,
                Cita = cita,
                Mensaje = "¡Tu cita ha sido confirmada con éxito!"
            };
        }

        if (apiResult.CodigoHttp == 409)
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.HoraOcupada,
                Mensaje = apiResult.Error ?? "La hora seleccionada ya fue ocupada por otro cliente."
            };
        }

        if (apiResult.SinConexion)
        {
            // La cita pendiente debe quedar a nombre del cliente de la sesión y con la duración real del servicio.
            var servicio = _servicioRepo is null ? null : await _servicioRepo.GetByIdAsync(solicitud.ServicioId);
            var duracionMin = servicio?.DuracionMin > 0 ? servicio.DuracionMin : 30;

            var citaLocal = new Cita
            {
                Id = citaId,
                ClienteId = _auth?.SesionActual?.UsuarioId ?? 0,
                BarberoId = solicitud.BarberoId,
                ServicioId = solicitud.ServicioId,
                Inicio = solicitud.Inicio,
                Fin = solicitud.Inicio.AddMinutes(duracionMin),
                Estado = EstadoCita.Pendiente,
                PendienteSincronizar = true
            };
            await _citaRepo.InsertOrUpdateAsync(citaLocal);
            await ProgramarRecordatorioAsync(citaLocal);
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.PendienteSinConexion,
                Cita = citaLocal,
                Mensaje = "Cita guardada en el dispositivo. Se sincronizará al recuperar conexión."
            };
        }

        return new ResultadoReserva
        {
            Tipo = ResultadoReservaTipo.Error,
            Mensaje = apiResult.Error ?? "Error al procesar la reserva."
        };
    }

    /// <summary>Cancela la cita en la API, actualiza su copia local y retira el aviso programado.</summary>
    public async Task<bool> CancelarAsync(Guid citaId)
    {
        var result = await _api.CancelarCitaAsync(citaId);
        var cita = await _citaRepo.GetByIdAsync(citaId);
        if (cita != null)
        {
            cita.Estado = EstadoCita.Cancelada;
            await _citaRepo.InsertOrUpdateAsync(cita);
        }

        // Una cita cancelada no debe seguir avisando.
        if (_reminders is not null)
            await _reminders.CancelarAsync(citaId);

        return result.Exito;
    }

    /// <summary>Programa el aviso local si el módulo de notificaciones está disponible.</summary>
    private async Task ProgramarRecordatorioAsync(Cita cita)
    {
        if (_reminders is not null)
            await _reminders.ProgramarAsync(cita);
    }
}
