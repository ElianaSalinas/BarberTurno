// Responsable: M01
// Alertas/confirmaciones desde ViewModels sin tocar la vista.
namespace BarberTurno.Services.Dialogs;

/// <summary>Contrato de diálogos para desacoplar los ViewModels de las páginas MAUI.</summary>
public interface IDialogService
{
    Task ShowAlertAsync(string titulo, string mensaje);
    Task<bool> ConfirmAsync(string titulo, string mensaje, string aceptar = "Aceptar", string cancelar = "Cancelar");
}

