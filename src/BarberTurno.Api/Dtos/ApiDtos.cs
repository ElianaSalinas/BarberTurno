namespace BarberTurno.Api.Dtos;

public class ActualizarFotoDto
{
    public string? FotoUrl { get; set; }
}

public class CrearServicioDto
{
    public string Nombre { get; set; } = "";
    public int DuracionMin { get; set; } = 30;
    public decimal Precio { get; set; }
}

public class LoginRequestDto
{
    public string Usuario { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegistroRequestDto
{
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Usuario { get; set; } = "";
    public string Password { get; set; } = "";
    public string Rol { get; set; } = "Cliente";
}

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public DateTime ExpiraEnUtc { get; set; }
    public string Rol { get; set; } = "";
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
}

public class ClienteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
}

public class BarberoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? FotoUrl { get; set; }
    public List<int> ServicioIds { get; set; } = new();
}

public class ServicioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int DuracionMin { get; set; }
    public decimal Precio { get; set; }
}

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

public class SlotDto
{
    public DateTime InicioUtc { get; set; }
    public DateTime FinUtc { get; set; }
}

public class CrearCitaRequestDto
{
    public Guid Id { get; set; }
    public int BarberoId { get; set; }
    public int ServicioId { get; set; }
    public DateTime InicioUtc { get; set; }
}
