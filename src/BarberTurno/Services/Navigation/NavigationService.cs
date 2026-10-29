// Responsable: M01
// Implementa INavigationService con Shell.Current.GoToAsync.
namespace BarberTurno.Services.Navigation;

public class NavigationService : INavigationService
{
    public async Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (Shell.Current is null) return;

        if (parameters is null || parameters.Count == 0)
        {
            await Shell.Current.GoToAsync(route);
        }
        else
        {
            await Shell.Current.GoToAsync(route, parameters);
        }
    }

    public async Task GoBackAsync()
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
