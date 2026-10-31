// Responsable: M01
// Abstracción de Shell para navegar desde los ViewModels.
namespace BarberTurno.Services.Navigation;

/// <summary>Permite a los ViewModels navegar mediante rutas Shell sin conocer las páginas.</summary>
public interface INavigationService
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
    Task GoBackAsync();
}

