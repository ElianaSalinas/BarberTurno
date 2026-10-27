using Microsoft.Extensions.DependencyInjection;
// Responsable: M07
// Registro de DI de red y sincronización.
namespace BarberTurno.Services.Sync;

public static class SyncServiceRegistration
{
    public static IServiceCollection AddSyncLayer(this IServiceCollection services)
    {
        // TODO(M07): INetworkService, ISyncService (Singleton).
        return services;
    }
}

