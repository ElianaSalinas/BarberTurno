using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

// Responsable: M01
// Implementa INavigationService con Shell.Current.GoToAsync y conmutación segura de pestañas.
namespace BarberTurno.Services.Navigation;

public class NavigationService : INavigationService
{
    public async Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (Shell.Current is null) return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                var shell = Shell.Current;
                if (shell is null) return;

                // 1. Si es navegación relativa hacia atrás ("..", "../..")
                if (route.StartsWith(".."))
                {
                    await shell.GoToAsync(route);
                    return;
                }

                // 2. Normalizar la ruta para comprobar si apunta a una pestaña
                var trimmed = route.TrimStart('/');
                var parts = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var targetTab = parts.Length > 0 ? parts[^1] : "";

                // 3. Comprobar si el destino es un ShellItem de nivel superior (ej: "//cliente", "//barbero", "//auth")
                foreach (var item in shell.Items)
                {
                    if (string.Equals(item.Route, trimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        shell.CurrentItem = item;
                        return;
                    }
                }

                // 4. Comprobar si el destino es una pestaña dentro de cualquier ShellItem
                foreach (var item in shell.Items)
                {
                    foreach (var section in item.Items)
                    {
                        foreach (var content in section.Items)
                        {
                            if (string.Equals(content.Route, targetTab, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(section.Route, targetTab, StringComparison.OrdinalIgnoreCase))
                            {
                                shell.CurrentItem = item;
                                item.CurrentItem = section;
                                section.CurrentItem = content;
                                return;
                            }
                        }
                    }
                }

                // 5. Si no es pestaña, es una página apilada (push route)
                if (parameters is null || parameters.Count == 0)
                {
                    await shell.GoToAsync(route);
                }
                else
                {
                    await shell.GoToAsync(route, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NavigationService] Error al navegar a '{route}': {ex}");
            }
        });
    }

    public async Task GoBackAsync()
    {
        if (Shell.Current is null) return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NavigationService] Error en GoBackAsync: {ex.Message}");
            }
        });
    }
}
