// Responsable: M01
// Implementa IDialogService con DisplayAlert de MAUI.
namespace BarberTurno.Services.Dialogs;

public class DialogService : IDialogService
{
    public async Task ShowAlertAsync(string titulo, string mensaje)
    {
        var pagina = ObtenerPaginaActiva();
        if (pagina != null)
        {
            await pagina.DisplayAlert(titulo, mensaje, "Aceptar");
        }
    }

    public async Task<bool> ConfirmAsync(string titulo, string mensaje, string aceptar = "Aceptar", string cancelar = "Cancelar")
    {
        var pagina = ObtenerPaginaActiva();
        if (pagina != null)
        {
            return await pagina.DisplayAlert(titulo, mensaje, aceptar, cancelar);
        }
        return false;
    }

    private static Page? ObtenerPaginaActiva()
    {
        if (Shell.Current?.CurrentPage != null)
            return Shell.Current.CurrentPage;

        if (Application.Current?.Windows.Count > 0)
            return Application.Current.Windows[0].Page;

        return null;
    }
}
