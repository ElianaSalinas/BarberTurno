using BarberTurno.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace BarberTurno.Services.Api;

public static class ApiServiceRegistration
{
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
