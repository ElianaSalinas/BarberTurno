using BarberTurno.Services.Availability;
using Microsoft.Extensions.DependencyInjection;
// Responsable: M08
// Registro de DI de disponibilidad y reserva.
namespace BarberTurno.Services.Booking;

public static class BookingServiceRegistration
{
    public static IServiceCollection AddBookingLayer(this IServiceCollection services)
    {
        // TODO(M07): IAvailabilityService, IBookingService.
        services.AddTransient<IAvailabilityService, AvailabilityService>();
        services.AddTransient<IBookingService, BookingService>();
        return services;
    }
}

