using Microsoft.Extensions.DependencyInjection;
using BarberTurno.Data;
using BarberTurno.Services.Api;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Availability;
using BarberTurno.Services.Booking;
using BarberTurno.Services.Dialogs;
using BarberTurno.Services.Navigation;
using BarberTurno.Services.Sync;
using BarberTurno.ViewModels;
using BarberTurno.Views;

// Responsable: M01 (composición de la app). Cada capa registra sus servicios en su propio archivo *Registration.cs.
namespace BarberTurno;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            // TODO(M01): .UseMauiCommunityToolkit() si se usa CommunityToolkit.Maui
            // TODO(M06): .UseLocalNotification() si se usa Plugin.LocalNotification
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services
            .AddDataLayer()          // M02
            .AddApiLayer()           // M05
            .AddAuthLayer()          // M06
            .AddSyncLayer()          // M07
            .AddBookingLayer()       // M08
            .AddPresentationLayer(); // M01 (ViewModels y páginas)

        return builder.Build();
    }

    private static IServiceCollection AddPresentationLayer(this IServiceCollection services)
    {
        // Servicios sin estado que envuelven Shell: una sola instancia para toda la app.
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        // ViewModels y páginas: Transient, cada navegación crea una pantalla nueva y limpia.
        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginPage>();
        services.AddTransient<BarberosViewModel>();
        services.AddTransient<BarberosPage>();
        services.AddTransient<ServiciosViewModel>();
        services.AddTransient<ServiciosPage>();
        services.AddTransient<SeleccionHoraViewModel>();
        services.AddTransient<SeleccionHoraPage>();
        services.AddTransient<ConfirmarReservaViewModel>();
        services.AddTransient<ConfirmarReservaPage>();
        services.AddTransient<MisCitasViewModel>();
        services.AddTransient<MisCitasPage>();
        services.AddTransient<PerfilViewModel>();
        services.AddTransient<PerfilPage>();
        services.AddTransient<AgendaDiaViewModel>();
        services.AddTransient<AgendaDiaPage>();
        services.AddTransient<HistorialClienteViewModel>();
        services.AddTransient<HistorialClientePage>();
        services.AddTransient<DetalleCitaViewModel>();
        services.AddTransient<DetalleCitaPage>();
        return services;
    }
}
