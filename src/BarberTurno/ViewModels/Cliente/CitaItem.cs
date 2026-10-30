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

    public string FechaTexto =>
        $"{FormatoCliente.FechaLarga(Inicio.ToLocalTime())} a las {FormatoCliente.Hora(Inicio.ToLocalTime())}";

    public CitaItem(Cita cita, string barberoNombre, string servicioNombre)
    {
        Cita = cita;
        BarberoNombre = barberoNombre;
        ServicioNombre = servicioNombre;
    }

    public bool EsProxima(DateTime ahora) =>
        Inicio.ToLocalTime() >= ahora &&
        Estado != EstadoCita.Cancelada &&
        Estado != EstadoCita.Rechazada;
}
