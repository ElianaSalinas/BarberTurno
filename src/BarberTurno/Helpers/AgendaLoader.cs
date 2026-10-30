using System.Globalization;
using BarberTurno.Data.Repositories;
using BarberTurno.Models;
using BarberTurno.ViewModels;

namespace BarberTurno.Helpers;

/// <summary>Resultado de cargar la agenda de un día: filas listas para mostrar, ingresos y próxima cita.</summary>
public sealed record AgendaResultado(List<CitaAgendaItem> Items, decimal Ingresos, CitaAgendaItem? Proxima);

/// <summary>
/// Lógica única de la agenda del barbero (la usan Mi agenda y Calendario).
/// Convierte Cita + Cliente + Servicio en filas listas para dibujar.
/// Reglas:
///  - El barbero se identifica con <see cref="SesionUsuario.BarberoId"/>, nunca con UsuarioId.
///  - <see cref="Cita.Inicio"/> ya está en hora local (DtoMapper convierte InicioUtc → local al guardar): no se reconvierte.
///  - No se muestran citas canceladas ni rechazadas.
/// </summary>
public static class AgendaLoader
{
    /// <summary>BarberoId de la sesión si es un barbero válido; null en cualquier otro caso (nunca devuelve otro Id).</summary>
    public static int? ObtenerBarberoId(SesionUsuario? sesion)
        => sesion is { Rol: RolUsuario.Barbero, BarberoId: > 0 } ? sesion.BarberoId : null;

    /// <returns>El resultado, o null si la sesión no tiene un perfil de barbero (BarberoId).</returns>
    public static async Task<AgendaResultado?> CargarAsync(
        SesionUsuario? sesion,
        DateOnly dia,
        DateTime ahora,
        ICitaRepository citas,
        IClienteRepository clientes,
        IServicioRepository servicios)
    {
        var barberoId = ObtenerBarberoId(sesion);
        if (barberoId is null)
            return null;

        var lista = (await citas.GetAgendaBarberoAsync(barberoId.Value, dia))
            .Where(c => c.Estado is not (EstadoCita.Cancelada or EstadoCita.Rechazada))
            .OrderBy(c => c.Inicio)
            .ToList();

        // Un solo lookup por cliente/servicio distinto (las citas de un día repiten pocos).
        var nombresCliente = new Dictionary<int, string?>();
        foreach (var id in lista.Select(c => c.ClienteId).Distinct())
            nombresCliente[id] = (await clientes.GetByIdAsync(id))?.Nombre;

        var catalogo = new Dictionary<int, Servicio?>();
        foreach (var id in lista.Select(c => c.ServicioId).Distinct())
            catalogo[id] = await servicios.GetByIdAsync(id);

        var items = new List<CitaAgendaItem>();
        decimal ingresos = 0m;
        CitaAgendaItem? proxima = null;
        var esHoy = dia == DateOnly.FromDateTime(ahora);

        foreach (var c in lista)
        {
            nombresCliente.TryGetValue(c.ClienteId, out var nombre);
            nombre = string.IsNullOrWhiteSpace(nombre) ? "Cliente" : nombre.Trim();
            catalogo.TryGetValue(c.ServicioId, out var serv);
            if (serv is not null) ingresos += serv.Precio;

            var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var iniciales = partes.Length >= 2 ? $"{partes[0][0]}{partes[1][0]}" : nombre[..1];
            var nombreServ = serv?.Nombre ?? "Servicio";

            var item = new CitaAgendaItem
            {
                Hora = c.Inicio.ToString("hh:mm", CultureInfo.InvariantCulture),
                Periodo = c.Inicio.ToString("tt", CultureInfo.InvariantCulture).ToUpperInvariant(),
                InicialesCliente = iniciales.ToUpperInvariant(),
                NombreCliente = nombre,
                Servicio = serv is null ? nombreServ : $"{nombreServ} · {serv.DuracionMin} min",
                Resumen = $"{nombre} · {nombreServ}"
            };
            items.Add(item);

            if (esHoy && proxima is null && c.Inicio >= ahora
                && c.Estado is EstadoCita.Confirmada or EstadoCita.Pendiente)
            {
                proxima = item;
            }
        }

        return new AgendaResultado(items, ingresos, proxima);
    }
}
