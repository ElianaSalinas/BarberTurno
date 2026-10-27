using BarberTurno.Models;
// Responsable: M08
// Orquesta la reserva: valida, guarda local, envía al servidor, programa recordatorio.
namespace BarberTurno.Services.Booking;

public interface IBookingService
{
    Task<ResultadoReserva> ReservarAsync(ReservaSolicitud solicitud);
    Task<bool> CancelarAsync(Guid citaId);   // opcional
}

