namespace BarberTurno.Api.Domain;

public class CitaBloqueEntity
{
    public int Id { get; set; }
    public int BarberoId { get; set; }
    public DateTime BloqueInicioUtc { get; set; }
    public Guid CitaId { get; set; }

    public CitaEntity? Cita { get; set; }
}
