namespace BarberTurno.Api.Domain;

/// <summary>Bloque de cinco minutos que protege una cita frente a reservas simultáneas.</summary>
public class CitaBloqueEntity
{
    public int Id { get; set; }
    public int BarberoId { get; set; }
    public DateTime BloqueInicioUtc { get; set; }
    public Guid CitaId { get; set; }

    public CitaEntity? Cita { get; set; }
}
