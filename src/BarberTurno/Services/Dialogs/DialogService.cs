// Responsable: M01
// Implementa IDialogService con DisplayAlertAsync de MAUI en el hilo principal y protegido con try/catch.
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace BarberTurno.Services.Dialogs;

public class DialogService : IDialogService
{
    public async Task ShowAlertAsync(string titulo, string mensaje)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var pagina = ObtenerPaginaActiva();
                if (pagina != null)
                {
                    await pagina.DisplayAlertAsync(titulo, mensaje, "Aceptar");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DialogService] Error al mostrar alerta: {ex}");
        }
    }

    public async Task<bool> ConfirmAsync(string titulo, string mensaje, string aceptar = "Aceptar", string cancelar = "Cancelar")
    {
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var pagina = ObtenerPaginaActiva();
                if (pagina != null)
                {
                    return await pagina.DisplayAlertAsync(titulo, mensaje, aceptar, cancelar);
                }
                return false;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DialogService] Error al mostrar confirmación: {ex}");
            return false;
        }
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
