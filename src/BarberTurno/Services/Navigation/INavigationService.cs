// Responsable: M01
// Abstracción de Shell para navegar desde los ViewModels.
namespace BarberTurno.Services.Navigation;

public interface INavigationService
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
    Task GoBackAsync();
}

