// Responsable: M02
// Objetos de transferencia = forma exacta del JSON de la API. Si la API cambia un campo, solo cambian Dtos/ y Mappers/.
using System.Text.Json.Serialization;

namespace BarberTurno.Dtos;

/// <summary>Datos enviados al servidor para iniciar sesión.</summary>
public class LoginRequestDto
{
    [JsonPropertyName("usuario")]
    public string Usuario { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}

/// <summary>Respuesta del servidor tras un login exitoso.</summary>
public class LoginResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("expiraEnUtc")]
    public DateTime ExpiraEnUtc { get; set; }

    [JsonPropertyName("rol")]
    public string Rol { get; set; } = "";

    [JsonPropertyName("usuarioId")]
    public int UsuarioId { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";
}

/// <summary>DTO de cliente que viaja por red.</summary>
public class ClienteDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";

    [JsonPropertyName("telefono")]
    public string Telefono { get; set; } = "";
}

/// <summary>DTO de barbero que viaja por red.</summary>
public class BarberoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";

    [JsonPropertyName("fotoUrl")]
    public string? FotoUrl { get; set; }

    [JsonPropertyName("servicioIds")]
    public List<int> ServicioIds { get; set; } = new();
}

/// <summary>DTO de servicio ofrecido en la barbería.</summary>
public class ServicioDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";

    [JsonPropertyName("duracionMin")]
    public int DuracionMin { get; set; }

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }
}

/// <summary>DTO de cita reservada con fechas en UTC y estado en texto.</summary>
public class CitaDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("clienteId")]
    public int ClienteId { get; set; }

    [JsonPropertyName("barberoId")]
    public int BarberoId { get; set; }

    [JsonPropertyName("servicioId")]
    public int ServicioId { get; set; }

    [JsonPropertyName("inicioUtc")]
    public DateTime InicioUtc { get; set; }

    [JsonPropertyName("finUtc")]
    public DateTime FinUtc { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "";

    [JsonPropertyName("motivoRechazo")]
    public string? MotivoRechazo { get; set; }
}

/// <summary>DTO de franja horaria disponible devuelta por el servidor.</summary>
public class SlotDto
{
    [JsonPropertyName("inicioUtc")]
    public DateTime InicioUtc { get; set; }

    [JsonPropertyName("finUtc")]
    public DateTime FinUtc { get; set; }
}

/// <summary>Id = clave de idempotencia: reintentar con el mismo Id nunca crea una segunda cita.</summary>
public class CrearCitaRequestDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("barberoId")]
    public int BarberoId { get; set; }

    [JsonPropertyName("servicioId")]
    public int ServicioId { get; set; }

    [JsonPropertyName("inicioUtc")]
    public DateTime InicioUtc { get; set; }
}
