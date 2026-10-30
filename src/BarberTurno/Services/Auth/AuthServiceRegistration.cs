// Responsable: M5
// Registro de DI de la capa de autenticación.
// IReminderService pertenece al módulo de notificaciones (otro responsable);
// su registro va en el archivo de ese módulo, no aquí.
using Microsoft.Extensions.DependencyInjection;

namespace BarberTurno.Services.Auth;

/// <summary>Registra el almacenamiento seguro y el servicio de autenticación en DI.</summary>
public static class AuthServiceRegistration
{
    /// <summary>Agrega implementaciones singleton que comparten la sesión activa en toda la app.</summary>
    public static IServiceCollection AddAuthLayer(this IServiceCollection services)
    {
        // Singleton: una sola instancia para toda la vida de la app.
        // ISessionStorage envuelve SecureStorage (sin estado propio → Singleton es seguro).
        services.AddSingleton<ISessionStorage, SessionStorage>();

        // Singleton: AuthService mantiene SesionActual en memoria.
        // Si fuera Transient, cada ViewModel recibiría una instancia distinta
        // y SesionActual sería null en todas menos la que hizo el login.
        services.AddSingleton<IAuthService, AuthService>();

        return services;
    }
}
