namespace BarberTurno.Api.Domain;

public class ServicioEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int DuracionMin { get; set; }
    public decimal Precio { get; set; }

    public ICollection<BarberoServicioEntity> BarberoServicios { get; set; } = new List<BarberoServicioEntity>();
}
