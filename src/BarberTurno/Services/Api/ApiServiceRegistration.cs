using Microsoft.Extensions.DependencyInjection;
// Responsable: M05
// Registro de DI de la capa de red.
namespace BarberTurno.Services.Api;

public static class ApiServiceRegistration
{
    public static IServiceCollection AddApiLayer(this IServiceCollection services)
    {
        // TODO(M05): HttpClient tipado (IBarberTurnoApi) con BaseAddress y timeout.
        return services;
    }
}

