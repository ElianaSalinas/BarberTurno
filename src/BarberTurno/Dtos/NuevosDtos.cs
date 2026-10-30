using System.Text.Json.Serialization;

namespace BarberTurno.Dtos;

/// <summary>Datos enviados para actualizar la foto de un barbero en la API.</summary>
public class ActualizarFotoDto
{
    [JsonPropertyName("fotoUrl")]
    public string? FotoUrl { get; set; }
}

/// <summary>Datos enviados para crear un servicio nuevo en el catálogo remoto.</summary>
public class CrearServicioDto
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";

    [JsonPropertyName("duracionMin")]
    public int DuracionMin { get; set; } = 30;

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }

    [JsonPropertyName("fotoUrl")]
    public string? FotoUrl { get; set; }
}
