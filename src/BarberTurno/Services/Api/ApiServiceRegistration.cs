using BarberTurno.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace BarberTurno.Services.Api;

/// <summary>Configura el HttpClient y el adaptador que conectan la app con la API REST.</summary>
public static class ApiServiceRegistration
{
    /// <summary>Registra un cliente HTTP compartido y su implementación de <see cref="IBarberTurnoApi"/>.</summary>
    public static IServiceCollection AddApiLayer(this IServiceCollection services)
    {
        services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri(AppConstants.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        });

        services.AddSingleton<IBarberTurnoApi, BarberTurnoApi>();

        return services;
    }
}
