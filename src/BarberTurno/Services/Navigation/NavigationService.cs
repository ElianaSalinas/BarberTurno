using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

// Responsable: M01
// Implementa INavigationService con SemaphoreSlim:
// 1. Descarta llamadas simultáneas (doble toque) con WaitAsync(0) sin encolar duplicados.
// 2. Aplica timeout de 5 s a la ejecución de la navegación misma para liberar el semáforo si Shell se cuelga.
// 3. Evita excepciones en GoBackAsync verificando que la pila no esté en la raíz de la pestaña.
namespace BarberTurno.Services.Navigation;

/// <summary>Centraliza la navegación Shell para que los ViewModels no dependan directamente de las páginas.</summary>
public class NavigationService : INavigationService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly TimeSpan NavigationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Navega a una ruta Shell y entrega sus parámetros al ViewModel destino.</summary>
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        return EjecutarAsync($"GoToAsync: {route}", async shell =>
        {
            // 1. Navegación hacia atrás relativa
            if (route.StartsWith(".."))
            {
                await shell.GoToAsync(route);
                return;
            }

            // 2. Normalizar ruta canónica
            var normalized = NormalizarRuta(route);

            // 3. Ejecutar navegación con o sin parámetros
            if (parameters is null || parameters.Count == 0)
            {
                await shell.GoToAsync(normalized);
            }
            else
            {
                await shell.GoToAsync(normalized, parameters);
            }
        });
    }

    /// <summary>Regresa una pantalla mediante la ruta relativa de Shell.</summary>
    public Task GoBackAsync()
    {
        return EjecutarAsync("GoBackAsync", async shell =>
        {
            // Evitar excepción de Shell al intentar retroceder si ya estamos en la raíz de la pestaña
            if (shell.Navigation.NavigationStack.Count <= 1 && shell.Navigation.ModalStack.Count == 0)
                return;

            await shell.GoToAsync("..");
        });
    }

    /// <summary>
    /// Ejecuta la navegación de forma atómica en el hilo principal con descarte de doble toque y límite de tiempo.
    /// </summary>
    private async Task EjecutarAsync(string descripcion, Func<Shell, Task> navegar)
    {
        if (Shell.Current is null) return;

        // Entrar con WaitAsync(0): si ya hay una navegación en curso, descarta la nueva para evitar duplicar páginas
        if (!await _semaphore.WaitAsync(0))
        {
            Debug.WriteLine($"[NavigationService] Ignorada (navegación en curso): '{descripcion}'");
            return;
        }

        try
        {
            // Aplicar el timeout a la navegación misma para que el semáforo se libere si Shell se cuelga en Android
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var shell = Shell.Current;
                if (shell is null) return;
                await navegar(shell);
            }).WaitAsync(NavigationTimeout);
        }
        catch (TimeoutException)
        {
            // Si la navegación tarda más de 5 s, se registra y el bloque finally asegura liberar el semáforo
            Debug.WriteLine($"[NavigationService] Timeout excedido ({NavigationTimeout.TotalSeconds}s) en '{descripcion}'. Se libera el semáforo.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NavigationService] Error al navegar en '{descripcion}': {ex}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static string NormalizarRuta(string route)
    {
        return route switch
        {
            "//barberos"       => "//cliente/barberos",
            "//miscitas"       => "//cliente/miscitas",
            "//inicio"         => "//cliente/inicio",
            "//catalogo"       => "//cliente/catalogo",
            "//perfilcliente"  => "//cliente/perfilcliente",
            "//agenda"         => "//barbero/agenda",
            "//perfilbarbero"  => "//barbero/perfilbarbero",
            "//login"          => "//auth/login",
            "//cliente"        => "//cliente/inicio",
            "//barbero"        => "//barbero/agenda",
            "//auth"           => "//auth/login",
            _                  => route
        };
    }
}
