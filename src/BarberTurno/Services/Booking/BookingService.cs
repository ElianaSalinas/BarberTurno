// Responsable: M08
// Orquesta la reserva: valida, guarda local, envía al servidor, programa recordatorio.
using BarberTurno.Data.Repositories;
using BarberTurno.Dtos;
using BarberTurno.Models;
using BarberTurno.Services.Api;

namespace BarberTurno.Services.Booking;

public class BookingService : IBookingService
{
    private readonly IBarberTurnoApi _api;
    private readonly ICitaRepository _citaRepo;

    public BookingService(IBarberTurnoApi api, ICitaRepository citaRepo)
    {
        _api = api;
        _citaRepo = citaRepo;
    }

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
            var citaLocal = new Cita
            {
                Id = citaId,
                ClienteId = 0,
                BarberoId = solicitud.BarberoId,
                ServicioId = solicitud.ServicioId,
                Inicio = solicitud.Inicio,
                Fin = solicitud.Inicio.AddMinutes(30),
                Estado = EstadoCita.Pendiente,
                PendienteSincronizar = true
            };
            await _citaRepo.InsertOrUpdateAsync(citaLocal);
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

    public async Task<bool> CancelarAsync(Guid citaId)
    {
        var result = await _api.CancelarCitaAsync(citaId);
        var cita = await _citaRepo.GetByIdAsync(citaId);
        if (cita != null)
        {
            cita.Estado = EstadoCita.Cancelada;
            await _citaRepo.InsertOrUpdateAsync(cita);
        }
        return result.Exito;
    }
}
