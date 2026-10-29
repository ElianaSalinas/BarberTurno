// Responsable: M07
// Implementa IBookingService.
using BarberTurno.Data.Repositories;
using BarberTurno.Dtos;
using BarberTurno.Models;
using BarberTurno.Services.Api;
using BarberTurno.Services.Auth;

namespace BarberTurno.Services.Booking;

public class BookingService : IBookingService
{
    private readonly IBarberoRepository _barberoRepository;
    private readonly IServicioRepository _servicioRepository;
    private readonly ICitaRepository _citaRepository;
    private readonly IBarberTurnoApi _api;
    private readonly IAuthService _authService;

    private static readonly TimeZoneInfo ZonaBarberia = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo");

    public BookingService(IBarberoRepository barberoRepository,IServicioRepository servicioRepository,ICitaRepository citaRepository,IBarberTurnoApi api,IAuthService authService)
    {
        _barberoRepository = barberoRepository;
        _servicioRepository = servicioRepository;
        _citaRepository = citaRepository;
        _api = api;
        _authService = authService;
    }

    public async Task<ResultadoReserva> ReservarAsync(ReservaSolicitud solicitud)
    {
        if (solicitud is null)
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Invalida,
                Mensaje = "La solicitud no puede estar vacía."
            };
        }

        var sesion = _authService.SesionActual;

        if (sesion is null ||sesion.UsuarioId <= 0 ||!EsCliente(sesion.Rol))
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Invalida,
                Mensaje = "Debes iniciar sesión como cliente."
            };
        }

        if (solicitud.BarberoId <= 0 ||solicitud.ServicioId <= 0)
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Invalida,
                Mensaje = "El barbero o servicio seleccionado no es válido."
            };
        }

        DateTime inicio;

        try
        {
            inicio = ConvertirHoraBarberia(solicitud.Inicio);
        }
        catch (Exception ex)
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Invalida,
                Mensaje = $"Fecha inválida: {ex.Message}"
            };
        }

        var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,ZonaBarberia);

        if (inicio <= ahora)
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Invalida,
                Mensaje = "No puedes reservar una fecha u hora pasada."
            };
        }

        Cita cita;

        try
        {
            var barbero = await _barberoRepository.GetByIdAsync(solicitud.BarberoId);

            if (barbero is null)
            {
                return new ResultadoReserva
                {
                    Tipo = ResultadoReservaTipo.Invalida,
                    Mensaje = "El barbero seleccionado no existe."
                };
            }

            var servicio = await _servicioRepository.GetByIdAsync(solicitud.ServicioId);

            if (servicio is null || servicio.DuracionMin <= 0)
            {
                return new ResultadoReserva
                {
                    Tipo = ResultadoReservaTipo.Invalida,
                    Mensaje = "El servicio seleccionado no es válido."
                };
            }

            var serviciosBarbero = await _servicioRepository.GetByBarberoAsync(solicitud.BarberoId);

            if (!serviciosBarbero.Any(s => s.Id == solicitud.ServicioId))
            {
                return new ResultadoReserva
                {
                    Tipo = ResultadoReservaTipo.Invalida,
                    Mensaje = "El barbero no ofrece este servicio."
                };
            }

            DateTime fin = inicio.AddMinutes(
                servicio.DuracionMin);

            var citasExistentes = await _citaRepository.GetAgendaBarberoAsync(solicitud.BarberoId,DateOnly.FromDateTime(inicio));

            bool existeConflicto = citasExistentes.Any(c =>
                c.BarberoId == solicitud.BarberoId &&
                EstadoBloqueaHorario(c.Estado) &&
                TieneSolapamiento(c.Inicio,c.Fin,inicio,fin));

            if (existeConflicto)
            {
                return new ResultadoReserva
                {
                    Tipo = ResultadoReservaTipo.HoraOcupada,
                    Mensaje = "Ya existe una reserva local en ese horario."
                };
            }

            cita = new Cita
            {
                Id = Guid.NewGuid(),
                ClienteId = sesion.UsuarioId,
                BarberoId = solicitud.BarberoId,
                ServicioId = solicitud.ServicioId,
                Inicio = inicio,
                Fin = fin,
                Estado = EstadoCita.Pendiente,
                MotivoRechazo = null
            };

            await _citaRepository.InsertAsync(cita);
        }
        catch (Exception ex)
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Error,
                Mensaje = $"No se pudo guardar la reserva: {ex.Message}"
            };
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return ResultadoPendiente(
                cita,
                "Reserva guardada. Pendiente de conexión.");
        }

        return await ReintentarReservaPendienteAsync(cita.Id);
    }

    public async Task<ResultadoReserva> ReintentarReservaPendienteAsync(
        Guid citaId)
    {
        Cita? cita;

        try
        {
            cita = await _citaRepository.GetByIdAsync(citaId);
        }
        catch (Exception ex)
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Error,
                Mensaje = $"No se pudo consultar: {ex.Message}"
            };
        }

        if (cita is null)
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Invalida,
                Mensaje = "La reserva no existe."
            };
        }

        if (cita.Estado == EstadoCita.Confirmada)
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Confirmada,
                Cita = cita,
                Mensaje = "La reserva ya está confirmada."
            };
        }

        if (cita.Estado != EstadoCita.Pendiente)
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Invalida,
                Cita = cita,
                Mensaje = "La reserva no está pendiente de confirmación."
            };
        }

        var sesion = _authService.SesionActual;

        if (sesion is null ||sesion.UsuarioId != cita.ClienteId ||!EsCliente(sesion.Rol))
        {
            return new ResultadoReserva
            {
                Tipo = ResultadoReservaTipo.Error,
                Cita = cita,
                Mensaje = "Debes iniciar sesión con el cliente que creó la reserva."
            };
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return ResultadoPendiente(
                cita,
                "Sin conexión. La reserva permanece pendiente.");
        }

        try
        {
            var solicitudApi = new CrearCitaRequestDto
            {
                Id = cita.Id,
                BarberoId = cita.BarberoId,
                ServicioId = cita.ServicioId,
                InicioUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(cita.Inicio,DateTimeKind.Unspecified),ZonaBarberia)
            };

            var resultado = await _api.CrearCitaAsync(solicitudApi);

            if (resultado.SinConexion)
            {
                return ResultadoPendiente(
                    cita,
                    "No se pudo contactar con el servidor. La reserva permanece pendiente.");
            }

            if (resultado.Exito)
            {
                if (resultado.Datos is null ||resultado.Datos.Id != cita.Id ||!string.Equals(resultado.Datos.Estado,nameof(EstadoCita.Confirmada),StringComparison.OrdinalIgnoreCase))
                {
                    return ResultadoPendiente(
                        cita,
                        "No se pudo verificar la confirmación del servidor.");
                }

                await _citaRepository.ActualizarEstadoAsync(
                    cita.Id,
                    EstadoCita.Confirmada);

                cita.Estado = EstadoCita.Confirmada;

                return new ResultadoReserva
                {
                    Tipo = ResultadoReservaTipo.Confirmada,
                    Cita = cita,
                    Mensaje = "Reserva confirmada correctamente."
                };
            }

            if (resultado.CodigoHttp == 409)
            {
                string motivo = resultado.Error
                    ?? "El horario ya fue reservado por otro cliente.";

                await _citaRepository.ActualizarEstadoAsync(cita.Id,EstadoCita.Rechazada,motivo);

                cita.Estado = EstadoCita.Rechazada;
                cita.MotivoRechazo = motivo;

                return new ResultadoReserva
                {
                    Tipo = ResultadoReservaTipo.HoraOcupada,
                    Cita = cita,
                    Mensaje = motivo
                };
            }

            if (resultado.CodigoHttp is 400 or 422)
            {
                string motivo = resultado.Error
                    ?? "El servidor rechazó los datos de la reserva.";

                await _citaRepository.ActualizarEstadoAsync(cita.Id,EstadoCita.Rechazada,motivo);

                cita.Estado = EstadoCita.Rechazada;
                cita.MotivoRechazo = motivo;

                return new ResultadoReserva
                {
                    Tipo = ResultadoReservaTipo.Invalida,
                    Cita = cita,
                    Mensaje = motivo
                };
            }

            return ResultadoPendiente(cita,resultado.Error ?? "No fue posible confirmar la reserva. Se reintentará.");
        }
        catch (Exception ex)
        {
            return ResultadoPendiente(
                cita,
                $"La reserva permanece pendiente en SQLite: {ex.Message}");
        }
    }

    public Task<bool> CancelarAsync(Guid citaId)
    {
        throw new NotSupportedException(
            "La cancelación de reservas todavía no está implementada.");
    }

    private static bool EstadoBloqueaHorario(EstadoCita estado)
    {
        return estado is
            EstadoCita.Pendiente or
            EstadoCita.Confirmada or
            EstadoCita.Atendida or
            EstadoCita.Ausente;
    }

    private static bool TieneSolapamiento(DateTime inicioExistente,DateTime finExistente,DateTime inicioNuevo,DateTime finNuevo)
    {
        return inicioNuevo < finExistente &&
               finNuevo > inicioExistente;
    }

    private static bool EsCliente(object rol)
    {
        return string.Equals(rol.ToString(),nameof(RolUsuario.Cliente),StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime ConvertirHoraBarberia(DateTime fecha)
    {
        return fecha.Kind switch
        {
            DateTimeKind.Utc =>
                TimeZoneInfo.ConvertTimeFromUtc(
                    fecha,
                    ZonaBarberia),

            DateTimeKind.Local =>
                TimeZoneInfo.ConvertTime(
                    fecha,
                    ZonaBarberia),

            _ => fecha
        };
    }

    private static ResultadoReserva ResultadoPendiente(Cita cita,string mensaje)
    {
        return new ResultadoReserva
        {
            Tipo = ResultadoReservaTipo.PendienteSinConexion,
            Cita = cita,
            Mensaje = mensaje
        };
    }

}
