using BarberTurno.Data.Repositories;
using BarberTurno.Models;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace BarberTurno.Services.Notifications;

/// <summary>
/// Responsable: M10.
/// Implementación del servicio de recordatorios y notificaciones locales.
/// 
/// ¿QUÉ HACE ESTE SERVICIO?
/// - Solicita permiso al usuario en tiempo de ejecución en Android 13+ (POST_NOTIFICATIONS).
/// - Programa una notificación local para que suene 1 hora antes de la hora de inicio de la cita.
/// - Cancela notificaciones programadas cuando una cita es Cancelada o Rechazada.
/// - Reprograma la alarma si el horario de la cita cambia.
/// - Reconcilia las notificaciones pendientes con la base de datos local SQLite al arrancar.
/// </summary>
public class ReminderService : IReminderService
{
    private readonly ICitaRepository _citaRepository;
    private readonly IServicioRepository _servicioRepository;
    private readonly ILogger<ReminderService> _logger;

    // Tiempo de anticipación para avisar al usuario antes de su cita (1 hora)
    private static readonly TimeSpan Anticipacion = TimeSpan.FromHours(1);

    public ReminderService(
        ICitaRepository citaRepository,
        IServicioRepository servicioRepository,
        ILogger<ReminderService> logger)
    {
        _citaRepository = citaRepository;
        _servicioRepository = servicioRepository;
        _logger = logger;
    }

    /// <summary>
    /// Solicita el permiso de notificaciones en tiempo de ejecución.
    /// Requerido formalmente en Android 13+ (API 33+) e iOS.
    /// Si el usuario deniega el permiso, devuelve false sin generar excepciones.
    /// </summary>
    public async Task<bool> SolicitarPermisoAsync()
    {
        try
        {
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled())
            {
                return true;
            }

            return await LocalNotificationCenter.Current.RequestNotificationPermission();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al solicitar permiso de notificaciones.");
            return false;
        }
    }

    /// <summary>
    /// Programa una notificación local en el sistema operativo antes de la hora de la cita.
    /// </summary>
    /// <param name="cita">Entidad Cita con su fecha y hora en UTC.</param>
    public async Task ProgramarAsync(Cita cita)
    {
        if (cita == null || cita.Id == Guid.Empty) return;

        // No programar avisos para citas que no estén activas
        if (cita.Estado == EstadoCita.Cancelada || cita.Estado == EstadoCita.Rechazada)
        {
            return;
        }

        DateTime inicioLocal = cita.Inicio.ToLocalTime();
        DateTime momentoNotificacion = inicioLocal.Subtract(Anticipacion);

        // Si la hora de notificación ya pasó (por ejemplo una cita para dentro de 10 minutos),
        // no intentamos programar en el pasado.
        if (momentoNotificacion <= DateTime.Now)
        {
            return;
        }

        try
        {
            // Verificar o solicitar permiso
            bool tienePermiso = await SolicitarPermisoAsync();
            if (!tienePermiso)
            {
                _logger.LogInformation("Permiso de notificaciones denegado; omitiendo programación.");
                return;
            }

            // Obtener el nombre del servicio para incluirlo en el mensaje
            var servicio = await _servicioRepository.GetByIdAsync(cita.ServicioId);
            string nombreServicio = servicio?.Nombre ?? "tu turno de barbería";

            int notificationId = GenerarIdNotificacion(cita.Id);

            var notification = new NotificationRequest
            {
                NotificationId = notificationId,
                Title = "Recordatorio de Turno - BarberTurno",
                Description = $"Tu cita para {nombreServicio} está programada para las {inicioLocal:hh:mm tt}.",
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = momentoNotificacion
                }
            };

            await LocalNotificationCenter.Current.Show(notification);
            _logger.LogInformation("Recordatorio programado para la cita {CitaId} a las {Hora}", cita.Id, momentoNotificacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo programar la notificación para la cita {CitaId}", cita.Id);
        }
    }

    /// <summary>
    /// Cancela una notificación previamente programada en el dispositivo mediante el Id de la cita.
    /// </summary>
    public Task CancelarAsync(Guid citaId)
    {
        if (citaId == Guid.Empty) return Task.CompletedTask;

        try
        {
            int notificationId = GenerarIdNotificacion(citaId);
            LocalNotificationCenter.Current.Cancel(notificationId);
            _logger.LogInformation("Notificación cancelada para la cita {CitaId}", citaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar la notificación para la cita {CitaId}", citaId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reprograma el recordatorio: cancela la alarma previa y crea una nueva con el nuevo horario.
    /// </summary>
    public async Task ReprogramarAsync(Cita cita)
    {
        if (cita == null) return;
        await CancelarAsync(cita.Id);
        await ProgramarAsync(cita);
    }

    /// <summary>
    /// Al arrancar la app, reconcilia las citas futuras en SQLite
    /// y reprograma sus recordatorios en el sistema operativo.
    /// </summary>
    public async Task ReconciliarAsync()
    {
        try
        {
            DateTime ahoraUtc = DateTime.UtcNow;

            // Consultar todas las citas que tienen inicio futuro
            var citasPendientes = await _citaRepository.GetPendientesAsync();
            foreach (var cita in citasPendientes.Where(c => c.Inicio > ahoraUtc))
            {
                await ProgramarAsync(cita);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error durante la reconciliación de recordatorios.");
        }
    }

    /// <summary>
    /// Convierte de forma determinista un Guid de 128 bits en un entero positivo de 32 bits
    /// que el sistema operativo pueda usar como NotificationId.
    /// </summary>
    private static int GenerarIdNotificacion(Guid citaId)
    {
        // Usamos GetHashCode asegurando que sea un número entero positivo
        int hash = citaId.GetHashCode();
        return hash == int.MinValue ? int.MaxValue : Math.Abs(hash);
    }
}