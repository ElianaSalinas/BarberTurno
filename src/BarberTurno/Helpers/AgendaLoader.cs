using System.Globalization;
using BarberTurno.Data.Repositories;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
using BarberTurno.ViewModels;

namespace BarberTurno.Helpers;

/// <summary>Convierte las citas reales de un barbero en filas listas para mostrar (cita + cliente + servicio).</summary>
public static class AgendaLoader
{
    public static async Task<(List<CitaAgendaItem> Items, decimal Ingresos)> CargarAsync(
        IAuthService auth, ICitaRepository citas, IClienteRepository clientes, IServicioRepository servicios, DateOnly dia)
    {
        var sesion = auth.SesionActual;
        if (sesion is null || sesion.Rol != RolUsuario.Barbero)
            return (new(), 0m);

        var lista = (await citas.GetAgendaBarberoAsync(sesion.UsuarioId, dia))
            .Where(c => c.Estado is EstadoCita.Pendiente or EstadoCita.Confirmada or EstadoCita.Atendida)
            .OrderBy(c => c.Inicio)
            .ToList();

        var nombresCliente = (await clientes.GetByIdsAsync(lista.Select(c => c.ClienteId).Distinct()))
            .ToDictionary(c => c.Id, c => c.Nombre);
        var catalogo = (await servicios.GetAllAsync()).ToDictionary(s => s.Id);

        var cultura = CultureInfo.GetCultureInfo("es");
        var items = new List<CitaAgendaItem>();
        decimal ingresos = 0m;

        foreach (var c in lista)
        {
            var local = DateTime.SpecifyKind(c.Inicio, DateTimeKind.Utc).ToLocalTime();
            nombresCliente.TryGetValue(c.ClienteId, out var nombre);
            nombre = string.IsNullOrWhiteSpace(nombre) ? "Cliente" : nombre;
            catalogo.TryGetValue(c.ServicioId, out var serv);
            if (serv is not null) ingresos += serv.Precio;

            var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var iniciales = partes.Length >= 2 ? $"{partes[0][0]}{partes[1][0]}" : nombre[..1];

            items.Add(new CitaAgendaItem
            {
                Hora = local.ToString("hh:mm", cultura),
                Periodo = local.ToString("tt", CultureInfo.InvariantCulture),
                InicialesCliente = iniciales.ToUpperInvariant(),
                NombreCliente = nombre,
                Servicio = serv is null ? "Servicio" : $"{serv.Nombre} · {serv.DuracionMin} min",
                Resumen = serv is null ? nombre : $"{nombre} · {serv.Nombre}"
            });
        }
        return (items, ingresos);
    }
}
