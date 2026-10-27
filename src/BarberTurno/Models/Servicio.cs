// Responsable: M02
// Entidad local Servicio.
namespace BarberTurno.Models;

public class Servicio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int DuracionMin { get; set; }
    public decimal Precio { get; set; }
}

