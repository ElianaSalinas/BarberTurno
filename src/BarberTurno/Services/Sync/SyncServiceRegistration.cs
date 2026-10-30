// Responsable: M07
// Registro de DI de red y sincronización.
using Microsoft.Extensions.DependencyInjection;
using BarberTurno.Services.Network;

namespace BarberTurno.Services.Sync;

/// <summary>Registra los servicios de conectividad y sincronización en el contenedor DI.</summary>
public static class SyncServiceRegistration
{
    /// <summary>Agrega implementaciones singleton para observar la red y sincronizar datos.</summary>
    public static IServiceCollection AddSyncLayer(this IServiceCollection services)
    {
        services.AddSingleton<INetworkService, NetworkService>();
        services.AddSingleton<ISyncService, SyncService>();

        return services;
    }
}
