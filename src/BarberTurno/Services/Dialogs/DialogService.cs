// Responsable: M01
// Implementa IDialogService.
namespace BarberTurno.Services.Dialogs;

public class DialogService : IDialogService
{
    public Task ShowAlertAsync(string titulo, string mensaje)
        => MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlert(titulo, mensaje, "Aceptar"));

    public Task<bool> ConfirmAsync(string titulo, string mensaje, string aceptar = "Aceptar", string cancelar = "Cancelar")
        => MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlert(titulo, mensaje, aceptar, cancelar));
}
