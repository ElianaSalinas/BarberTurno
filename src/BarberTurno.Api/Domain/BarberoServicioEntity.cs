namespace BarberTurno.Api.Domain;

public class BarberoServicioEntity
{
    public int BarberoId { get; set; }
    public BarberoEntity Barbero { get; set; } = null!;

    public int ServicioId { get; set; }
    public ServicioEntity Servicio { get; set; } = null!;
}
