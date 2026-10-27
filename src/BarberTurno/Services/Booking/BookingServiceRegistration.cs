using Microsoft.Extensions.DependencyInjection;
// Responsable: M08
// Registro de DI de disponibilidad y reserva.
namespace BarberTurno.Services.Booking;

public static class BookingServiceRegistration
{
    public static IServiceCollection AddBookingLayer(this IServiceCollection services)
    {
        // TODO(M08): IAvailabilityService, IBookingService.
        return services;
    }
}

