// Responsable: M05
// Objetos de transferencia = forma exacta del JSON de la API. Si la API cambia un campo, solo cambian Dtos/ y Mappers/.
namespace BarberTurno.Dtos;

public class LoginRequestDto { public string Usuario { get; set; } = ""; public string Password { get; set; } = ""; }

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public DateTime ExpiraEnUtc { get; set; }
    public string Rol { get; set; } = "";
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
}

public class ClienteDto { public int Id { get; set; } public string Nombre { get; set; } = ""; public string Telefono { get; set; } = ""; }

public class BarberoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? FotoUrl { get; set; }
    public List<int> ServicioIds { get; set; } = new();
}

public class ServicioDto { public int Id { get; set; } public string Nombre { get; set; } = ""; public int DuracionMin { get; set; } public decimal Precio { get; set; } }

public class CitaDto
{
    public Guid Id { get; set; }
    public int ClienteId { get; set; }
    public int BarberoId { get; set; }
    public int ServicioId { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime FinUtc { get; set; }
    public string Estado { get; set; } = "";
    public string? MotivoRechazo { get; set; }
}

public class SlotDto { public DateTime InicioUtc { get; set; } public DateTime FinUtc { get; set; } }

/// <summary>Id = clave de idempotencia: reintentar con el mismo Id nunca crea una segunda cita.</summary>
public class CrearCitaRequestDto { public Guid Id { get; set; } public int BarberoId { get; set; } public int ServicioId { get; set; } public DateTime InicioUtc { get; set; } }

