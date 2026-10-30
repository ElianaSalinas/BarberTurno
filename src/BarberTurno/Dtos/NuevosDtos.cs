using System.Text.Json.Serialization;

namespace BarberTurno.Dtos;

public class ActualizarFotoDto
{
    [JsonPropertyName("fotoUrl")]
    public string? FotoUrl { get; set; }
}

public class CrearServicioDto
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";

    [JsonPropertyName("duracionMin")]
    public int DuracionMin { get; set; } = 30;

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }
}
