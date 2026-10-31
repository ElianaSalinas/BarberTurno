namespace BarberTurno.Api.Dtos;

/// <summary>Datos enviados para actualizar la foto de un barbero.</summary>
public class ActualizarFotoDto
{
    public string? FotoUrl { get; set; }
}

/// <summary>Datos necesarios para crear un servicio del catálogo.</summary>
public class CrearServicioDto
{
    public string Nombre { get; set; } = "";
    public int DuracionMin { get; set; } = 30;
    public decimal Precio { get; set; }
    public string? FotoUrl { get; set; }
}

/// <summary>Credenciales que la aplicación envía al iniciar sesión.</summary>
public class LoginRequestDto
{
    public string Usuario { get; set; } = "";
    public string Password { get; set; } = "";
}

/// <summary>Datos para crear un usuario y, si corresponde, su perfil de barbero.</summary>
public class RegistroRequestDto
{
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Usuario { get; set; } = "";
    public string Password { get; set; } = "";
    public string Rol { get; set; } = "Cliente";
}

/// <summary>Sesión autenticada que la API devuelve a la aplicación móvil.</summary>
public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public DateTime ExpiraEnUtc { get; set; }
    public string Rol { get; set; } = "";
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public int? BarberoId { get; set; }
}

/// <summary>Datos públicos de un cliente para las pantallas del barbero.</summary>
public class ClienteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
}

/// <summary>Barbero y los identificadores de los servicios que ofrece.</summary>
public class BarberoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? FotoUrl { get; set; }
    public List<int> ServicioIds { get; set; } = new();
}

/// <summary>Servicio del catálogo expuesto a la aplicación cliente.</summary>
public class ServicioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int DuracionMin { get; set; }
    public decimal Precio { get; set; }
    public string? FotoUrl { get; set; }
}

/// <summary>Representación de una cita transferida entre la API y la aplicación.</summary>
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

/// <summary>Intervalo horario libre calculado para una reserva.</summary>
public class SlotDto
{
    public DateTime InicioUtc { get; set; }
    public DateTime FinUtc { get; set; }
}

/// <summary>Solicitud de reserva enviada por la aplicación autenticada.</summary>
public class CrearCitaRequestDto
{
    public Guid Id { get; set; }
    public int BarberoId { get; set; }
    public int ServicioId { get; set; }
    public DateTime InicioUtc { get; set; }
}
