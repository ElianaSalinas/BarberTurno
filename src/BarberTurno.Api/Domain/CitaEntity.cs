namespace BarberTurno.Api.Domain;

public class CitaEntity
{
    public Guid Id { get; set; }
    public int ClienteId { get; set; }
    public int BarberoId { get; set; }
    public int ServicioId { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime FinUtc { get; set; }
    public string Estado { get; set; } = "Confirmada"; // Confirmada, Pendiente, Rechazada, Cancelada
    public string? MotivoRechazo { get; set; }

    public UsuarioEntity? Cliente { get; set; }
    public BarberoEntity? Barbero { get; set; }
    public ServicioEntity? Servicio { get; set; }
}
