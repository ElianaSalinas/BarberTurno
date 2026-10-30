using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

// Responsable: M01
// Implementa INavigationService seguro contra deadlocks en el hilo principal y fallos de rutas.
namespace BarberTurno.Services.Navigation;

public class NavigationService : INavigationService
{
    // Anti-rebote: ignora navegación si la última fue hace menos de 400 ms.
    private static DateTime _ultimaNavegacion = DateTime.MinValue;
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(400);
    private static bool _navegando;

    public async Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (Shell.Current is null) return;

        // Evitar doble-tap: ignora si ya hay una navegación activa o si fue muy reciente.
        var ahora = DateTime.UtcNow;
        if (_navegando || (ahora - _ultimaNavegacion) < Debounce)
        {
            Debug.WriteLine($"[NavigationService] Navegación ignorada (debounce): '{route}'");
            return;
        }

        try
        {
            _navegando = true;
            _ultimaNavegacion = ahora;

            if (MainThread.IsMainThread)
            {
                await ExecuteGoToAsync(route, parameters);
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() => ExecuteGoToAsync(route, parameters));
            }
        }
        finally
        {
            _navegando = false;
        }
    }

    private static async Task ExecuteGoToAsync(string route, IDictionary<string, object>? parameters)
    {
        try
        {
            if (Shell.Current is null) return;

            // Normalizar alias comunes a rutas canónicas de Shell
            var normalized = NormalizarRuta(route);

            Debug.WriteLine($"[NavigationService] Navegando: '{route}' → '{normalized}'");

            if (parameters is null || parameters.Count == 0)
            {
                await Shell.Current.GoToAsync(normalized, animate: true);
            }
            else
            {
                await Shell.Current.GoToAsync(normalized, animate: true, parameters);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NavigationService] Error al navegar a '{route}': {ex}");
        }
    }

    private static string NormalizarRuta(string route)
    {
        return route switch
        {
            // Alias directos a rutas canónicas
            "//barberos"       => "//cliente/barberos",
            "//miscitas"       => "//cliente/miscitas",
            "//inicio"         => "//cliente/inicio",
            "//catalogo"       => "//cliente/catalogo",
            "//perfilcliente"  => "//cliente/perfilcliente",
            "//agenda"         => "//barbero/agenda",
            "//perfilbarbero"  => "//barbero/perfilbarbero",
            "//login"          => "//auth/login",

            // TabBar routes: redirigir al primer tab explícito
            "//cliente"        => "//cliente/inicio",
            "//barbero"        => "//barbero/agenda",
            "//auth"           => "//auth/login",

            _                  => route
        };
    }

    public async Task GoBackAsync()
    {
        if (Shell.Current is null) return;

        if (MainThread.IsMainThread)
        {
            await ExecuteGoBackAsync();
        }
        else
        {
            await MainThread.InvokeOnMainThreadAsync(ExecuteGoBackAsync);
        }
    }

    private static async Task ExecuteGoBackAsync()
    {
        try
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NavigationService] Error en GoBackAsync: {ex}");
        }
    }
}
