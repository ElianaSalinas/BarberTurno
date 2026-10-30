// Responsable: M07
// Registro de DI de red y sincronización.
using Microsoft.Extensions.DependencyInjection;
using BarberTurno.Services.Network;

namespace BarberTurno.Services.Sync;

public static class SyncServiceRegistration
{
    public static IServiceCollection AddSyncLayer(this IServiceCollection services)
    {
        services.AddSingleton<INetworkService, NetworkService>();
        services.AddSingleton<ISyncService, SyncService>();

        return services;
    }
}