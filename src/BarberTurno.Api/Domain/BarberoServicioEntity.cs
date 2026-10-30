namespace BarberTurno.Api.Domain;

/// <summary>Entidad puente que relaciona los servicios ofrecidos por cada barbero.</summary>
public class BarberoServicioEntity
{
    public int BarberoId { get; set; }
    public BarberoEntity Barbero { get; set; } = null!;

    public int ServicioId { get; set; }
    public ServicioEntity Servicio { get; set; } = null!;
}
