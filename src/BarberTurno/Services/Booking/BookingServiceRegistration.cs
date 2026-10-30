using BarberTurno.Services.Availability;
using Microsoft.Extensions.DependencyInjection;
// Responsable: M08
// Registro de DI de disponibilidad y reserva.
namespace BarberTurno.Services.Booking;

/// <summary>Registra los servicios de disponibilidad y reserva usados por los ViewModels.</summary>
public static class BookingServiceRegistration
{
    /// <summary>Agrega implementaciones transitorias que consultan horarios y procesan citas.</summary>
    public static IServiceCollection AddBookingLayer(this IServiceCollection services)
    {
        // TODO(M07): IAvailabilityService, IBookingService.
        services.AddTransient<IAvailabilityService, AvailabilityService>();
        services.AddTransient<IBookingService, BookingService>();
        return services;
    }
}

