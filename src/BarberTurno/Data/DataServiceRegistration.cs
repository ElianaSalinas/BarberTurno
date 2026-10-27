using Microsoft.Extensions.DependencyInjection;
// Responsable: M02
// Registro de DI de la capa de datos (cada responsable edita SU archivo para evitar conflictos).
namespace BarberTurno.Data;

public static class DataServiceRegistration
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services)
    {
        // TODO(M02): AppDatabase y repositorios como Singleton.
        return services;
    }
}

