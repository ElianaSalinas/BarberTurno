using Microsoft.Extensions.DependencyInjection;
// Responsable: M06
// Registro de DI de autenticación y notificaciones.
namespace BarberTurno.Services.Auth;

public static class AuthServiceRegistration
{
    public static IServiceCollection AddAuthLayer(this IServiceCollection services)
    {
        // TODO(M06): ISessionStorage, IAuthService, IReminderService.
        return services;
    }
}

