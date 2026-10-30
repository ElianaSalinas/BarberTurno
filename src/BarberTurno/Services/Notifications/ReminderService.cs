// Responsable: M06
// Recordatorios locales de citas con Plugin.LocalNotification (ver ADR-0009).
// El id de cada notificación se deriva del Id (Guid) de la cita: así se puede cancelar o reprogramar sin guardar nada extra.
using System.Diagnostics;
using BarberTurno.Data.Repositories;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
#if ANDROID || IOS || MACCATALYST || WINDOWS
using Plugin.LocalNotification;
#endif

namespace BarberTurno.Services.Notifications;

public class ReminderService : IReminderService
{
    /// <summary>Con cuánta anticipación se avisa de la cita.</summary>
    public static readonly TimeSpan Anticipacion = TimeSpan.FromMinutes(60);

    private readonly ICitaRepository _citas;
    private readonly IBarberoRepository _barberos;
    private readonly IServicioRepository _servicios;
    private readonly IAuthService _auth;

    public ReminderService(
        ICitaRepository citas,
        IBarberoRepository barberos,
        IServicioRepository servicios,
        IAuthService auth)
    {
        _citas = citas;
        _barberos = barberos;
        _servicios = servicios;
        _auth = auth;
    }

    /// <summary>Id numérico estable de la notificación a partir del Guid de la cita.</summary>
    public static int IdNotificacion(Guid citaId)
        => BitConverter.ToInt32(citaId.ToByteArray(), 0) & int.MaxValue;

    /// <summary>
    /// Cuándo debe sonar el aviso: <see cref="Anticipacion"/> antes de la cita; si ya falta menos,
    /// en unos segundos; null si la cita ya empezó.
    /// </summary>
    public static DateTime? CalcularMomentoAviso(DateTime inicioLocal, DateTime ahora)
    {
        if (inicioLocal <= ahora)
            return null;

        var aviso = inicioLocal - Anticipacion;
        return aviso > ahora ? aviso : ahora.AddSeconds(10);
    }

    public async Task<bool> SolicitarPermisoAsync()
    {
        try
        {
#if ANDROID || IOS || MACCATALYST || WINDOWS
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled())
                return true;

            return await LocalNotificationCenter.Current.RequestNotificationPermission();
#else
            return await Task.FromResult(false);
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReminderService] No se pudo pedir el permiso: {ex.Message}");
            return false;
        }
    }

    public async Task ProgramarAsync(Cita cita)
    {
        try
        {
            if (cita.Estado is not (EstadoCita.Pendiente or EstadoCita.Confirmada))
            {
                await CancelarAsync(cita.Id);
                return;
            }

            var momento = CalcularMomentoAviso(cita.Inicio, DateTime.Now);
            if (momento is null)
            {
                await CancelarAsync(cita.Id);
                return;
            }

            if (!await SolicitarPermisoAsync())
                return; // sin permiso no se programa nada; la cita sigue siendo válida

            var barbero = await _barberos.GetByIdAsync(cita.BarberoId);
            var servicio = await _servicios.GetByIdAsync(cita.ServicioId);
            var titulo = "Tu turno en BarberTurno";
            var detalle = $"{servicio?.Nombre ?? "Tu servicio"} con {barbero?.Nombre ?? "tu barbero"} a las {cita.Inicio:hh:mm tt}";

#if ANDROID || IOS || MACCATALYST || WINDOWS
            var solicitud = new NotificationRequest
            {
                NotificationId = IdNotificacion(cita.Id),
                Title = titulo,
                Description = detalle,
                Schedule = { NotifyTime = momento.Value }
            };
            await LocalNotificationCenter.Current.Show(solicitud);
#else
            await Task.CompletedTask;
#endif
        }
        catch (Exception ex)
        {
            // El recordatorio nunca debe impedir una reserva.
            Debug.WriteLine($"[ReminderService] No se pudo programar el recordatorio de {cita.Id}: {ex.Message}");
        }
    }

    public Task CancelarAsync(Guid citaId)
    {
        try
        {
#if ANDROID || IOS || MACCATALYST || WINDOWS
            LocalNotificationCenter.Current.Cancel(IdNotificacion(citaId));
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReminderService] No se pudo cancelar el recordatorio de {citaId}: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    public async Task ReprogramarAsync(Cita cita)
    {
        await CancelarAsync(cita.Id);
        await ProgramarAsync(cita);
    }

    /// <summary>
    /// Alinea los recordatorios con las citas vigentes del cliente: programa las que faltan
    /// y cancela las de citas canceladas, rechazadas o pasadas. Se llama tras sincronizar.
    /// </summary>
    public async Task ReconciliarAsync()
    {
        try
        {
            var sesion = _auth.SesionActual;
            if (sesion is null || sesion.Rol != RolUsuario.Cliente)
                return;

            var lista = await _citas.GetByClienteAsync(sesion.UsuarioId);
            foreach (var cita in lista)
                await ProgramarAsync(cita); // ProgramarAsync cancela solo si ya no corresponde
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReminderService] Error al reconciliar recordatorios: {ex.Message}");
        }
    }
}

public static class NotificationServiceRegistration
{
    public static IServiceCollection AddNotificationLayer(this IServiceCollection services)
    {
        services.AddSingleton<IReminderService, ReminderService>();
        return services;
    }
}
