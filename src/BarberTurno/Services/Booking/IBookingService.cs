using BarberTurno.Models;
// Responsable: M08
// Orquesta la reserva: valida, guarda local, envía al servidor, programa recordatorio.
namespace BarberTurno.Services.Booking;

/// <summary>Contrato para crear y cancelar reservas desde la interfaz de la aplicación.</summary>
public interface IBookingService
{
    Task<ResultadoReserva> ReservarAsync(ReservaSolicitud solicitud);
    Task<bool> CancelarAsync(Guid citaId);   // opcional
}

