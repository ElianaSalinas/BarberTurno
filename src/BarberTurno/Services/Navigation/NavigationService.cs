// Responsable: M01
// Implementa INavigationService con Shell.Current.GoToAsync.
namespace BarberTurno.Services.Navigation;

public class NavigationService : INavigationService
{
    // Usar las constantes de Helpers/Routes.cs: Routes.Cliente / Routes.Barbero / Routes.Login (raíz, "//")
    // o páginas apiladas (Routes.Servicios, ...). Los parámetros llegan al ViewModel con [QueryProperty] o IQueryAttributable.
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        => MainThread.InvokeOnMainThreadAsync(() => parameters is null
            ? Shell.Current.GoToAsync(route)
            : Shell.Current.GoToAsync(route, parameters));

    public Task GoBackAsync()
        => MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(".."));
}
