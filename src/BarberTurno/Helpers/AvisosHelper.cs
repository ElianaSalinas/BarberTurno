using BarberTurno.Data.Repositories;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace BarberTurno.Helpers;

/// <summary>Campana de avisos: pide permiso de notificaciones y muestra los próximos turnos.</summary>
public static class AvisosHelper
{
    public static async Task MostrarAsync(IServiceProvider? servicios)
    {
        try
        {
            var pagina = Shell.Current?.CurrentPage;
            if (pagina is null) return;

            var estado = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (estado != PermissionStatus.Granted)
                estado = await Permissions.RequestAsync<Permissions.PostNotifications>();

            var sesion = servicios?.GetService<IAuthService>()?.SesionActual;
            var citas = servicios?.GetService<ICitaRepository>();

            var esBarbero = sesion?.Rol == RolUsuario.Barbero;
            var barberoId = AgendaLoader.ObtenerBarberoId(sesion);
            string cuerpo;

            if (sesion is null || citas is null)
            {
                cuerpo = "No hay una sesión activa.";
            }
            else if (esBarbero && barberoId is null)
            {
                // Nunca se usa otro Id: sin BarberoId no se consulta nada.
                cuerpo = "No pudimos identificar tu perfil de barbero. Cierra sesión e inicia de nuevo.";
            }
            else
            {
                var ahora = DateTime.Now; // Cita.Inicio se guarda en hora local
                var lista = esBarbero
                    ? await citas.GetAgendaBarberoAsync(barberoId!.Value, DateOnly.FromDateTime(ahora))
                    : await citas.GetProximasClienteAsync(sesion.UsuarioId, ahora);

                var lineas = lista
                    .Where(c => c.Estado is not (EstadoCita.Cancelada or EstadoCita.Rechazada))
                    .OrderBy(c => c.Inicio)
                    .Take(5)
                    .Select(c => $"{c.Inicio:ddd dd/MM · HH:mm}")
                    .ToList();

                cuerpo = lineas.Count > 0
                    ? (esBarbero ? "Tus turnos de hoy:\n\n" : "Tus próximos turnos:\n\n") + string.Join("\n", lineas)
                    : (esBarbero ? "No tienes turnos para hoy." : "No tienes turnos próximos por ahora.");
            }

            if (estado != PermissionStatus.Granted)
                cuerpo += "\n\nLos recordatorios están desactivados: actívalos en Ajustes del teléfono.";

            await pagina.DisplayAlert("Avisos", cuerpo, "Cerrar");
        }
        catch
        {
            // Función auxiliar: nunca debe cerrar la app.
        }
    }
}
