namespace BarberTurno.Api.Domain;

public class BarberoEntity
{
    public int Id { get; set; }
    public int? UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }

    public UsuarioEntity? Usuario { get; set; }
    public ICollection<BarberoServicioEntity> BarberoServicios { get; set; } = new List<BarberoServicioEntity>();
}
