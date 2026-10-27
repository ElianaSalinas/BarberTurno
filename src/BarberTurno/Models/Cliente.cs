// Responsable: M02
// Entidad local Cliente. TODO: atributos de sqlite-net ([PrimaryKey], etc.).
namespace BarberTurno.Models;

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string? FotoRuta { get; set; }
}

