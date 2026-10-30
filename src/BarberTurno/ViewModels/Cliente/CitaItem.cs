using BarberTurno.Helpers;
using BarberTurno.Models;

namespace BarberTurno.ViewModels;

public sealed class CitaItem
{
    public Cita Cita { get; }
    public string BarberoNombre { get; }
    public string ServicioNombre { get; }

    public Guid Id => Cita.Id;
    public DateTime Inicio => Cita.Inicio;
    public EstadoCita Estado => Cita.Estado;
    public string? MotivoRechazo => Cita.MotivoRechazo;
    public bool TieneMotivo => !string.IsNullOrWhiteSpace(MotivoRechazo);

    public bool PuedeCancelar =>
        Estado == EstadoCita.Pendiente || Estado == EstadoCita.Confirmada;

    // Cita.Inicio ya está en hora local (DtoMapper convierte InicioUtc → local al guardar).
    // Al leerla de SQLite llega con Kind=Unspecified: llamar a ToLocalTime() la trataría como UTC y la desplazaría horas.
    public string FechaTexto =>
        $"{FormatoCliente.FechaLarga(Inicio)} a las {FormatoCliente.Hora(Inicio)}";

    public CitaItem(Cita cita, string barberoNombre, string servicioNombre)
    {
        Cita = cita;
        BarberoNombre = barberoNombre;
        ServicioNombre = servicioNombre;
    }

    public bool EsProxima(DateTime ahora) =>
        Inicio >= ahora &&
        Estado != EstadoCita.Cancelada &&
        Estado != EstadoCita.Rechazada;
}
