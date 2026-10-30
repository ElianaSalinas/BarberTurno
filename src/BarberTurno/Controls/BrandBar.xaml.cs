using BarberTurno.Data.Repositories;
using BarberTurno.Models;
using BarberTurno.Services.Auth;

namespace BarberTurno.Controls;

public partial class BrandBar : ContentView
{
    public BrandBar() => InitializeComponent();

    // Campana de avisos: pide permiso de notificaciones (Android 13+) y muestra los próximos turnos.
    private async void OnCampanaTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            // Pequeño "toque" visual
            await CampanaBorder.ScaleToAsync(0.88, 70, Easing.CubicOut);
            await CampanaBorder.ScaleToAsync(1, 110, Easing.CubicOut);

            var pagina = Shell.Current?.CurrentPage;
            if (pagina is null) return;

            var estado = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (estado != PermissionStatus.Granted)
                estado = await Permissions.RequestAsync<Permissions.PostNotifications>();

            var servicios = Handler?.MauiContext?.Services;
            var auth = servicios?.GetService<IAuthService>();
            var citas = servicios?.GetService<ICitaRepository>();
            var sesion = auth?.SesionActual;

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
                ? "Tus próximos turnos:\n\n" + string.Join("\n", lineas)
                : "No tienes turnos próximos por ahora.";

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
