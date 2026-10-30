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

            var lineas = new List<string>();
            if (sesion is not null && citas is not null)
            {
                var lista = sesion.Rol == RolUsuario.Cliente
                    ? await citas.GetProximasClienteAsync(sesion.UsuarioId, DateTime.UtcNow)
                    : await citas.GetAgendaBarberoAsync(sesion.UsuarioId, DateOnly.FromDateTime(DateTime.Now));

                foreach (var c in lista.OrderBy(c => c.Inicio).Take(5))
                {
                    var local = DateTime.SpecifyKind(c.Inicio, DateTimeKind.Utc).ToLocalTime();
                    lineas.Add($"{local:ddd dd/MM · HH:mm}");
                }
            }

            var cuerpo = lineas.Count > 0
                ? (sesion?.Rol == RolUsuario.Barbero ? "Tus turnos de hoy:\n\n" : "Tus próximos turnos:\n\n") + string.Join("\n", lineas)
                : (sesion?.Rol == RolUsuario.Barbero ? "No tienes turnos para hoy." : "No tienes turnos próximos por ahora.");

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
