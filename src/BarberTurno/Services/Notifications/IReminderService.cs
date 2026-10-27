using BarberTurno.Models;
// Responsable: M06
// Recordatorios locales de citas (con permiso).
namespace BarberTurno.Services.Notifications;

public interface IReminderService
{
    Task<bool> SolicitarPermisoAsync();
    Task ProgramarAsync(Cita cita);        // id de notificación derivado del Id de la cita
    Task CancelarAsync(Guid citaId);
    Task ReprogramarAsync(Cita cita);      // cancelar + programar
    Task ReconciliarAsync();               // al iniciar: alinear recordatorios con las citas vigentes
}

