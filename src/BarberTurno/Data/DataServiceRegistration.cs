using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;

// Responsable: Módulo 3 · Base de datos SQLite (base de datos) y módulo 4 (repositorios)
// Registro de DI de la capa de datos (cada responsable edita SU bloque para evitar conflictos).
// MauiProgram (módulo 1) ya llama a AddDataLayer(); no hace falta tocarlo.
namespace BarberTurno.Data;

public static class DataServiceRegistration
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services)
    {
        // ---------- Módulo 3 · Base de datos ----------
        // Único punto de la capa de datos que conoce MAUI: decide dónde vive el archivo.
        services.AddSingleton(_ => new DatabaseOptions(
            RutaArchivo: Path.Combine(FileSystem.Current.AppDataDirectory, DatabaseOptions.NombreArchivoPorDefecto),
#if DEBUG
            SembrarDatosDePrueba: false));
#else
            SembrarDatosDePrueba: false));
#endif

        // Singleton: una sola conexión compartida por toda la app (recomendado por sqlite-net).
        services.AddSingleton<IAppDatabase, AppDatabase>();

        // ---------- Módulo 4 · Repositorios (Singleton) ----------
        // Singleton: no guardan estado propio y comparten la única conexión de IAppDatabase.
        services.AddSingleton<Repositories.IBarberoRepository, Repositories.BarberoRepository>();
        services.AddSingleton<Repositories.IServicioRepository, Repositories.ServicioRepository>();
        services.AddSingleton<Repositories.IClienteRepository, Repositories.ClienteRepository>();
        services.AddSingleton<Repositories.ICitaRepository, Repositories.CitaRepository>();

        return services;
    }
}
