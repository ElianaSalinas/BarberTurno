using BarberTurno.Models;

namespace BarberTurno.ViewModels;

public sealed class ServicioItem
{
    public int Id { get; }
    public string Nombre { get; }
    public int DuracionMin { get; }
    public decimal Precio { get; }

    public string DuracionTexto => $"{DuracionMin} min";
    public string PrecioTexto => $"${Precio:N2}";

    public ServicioItem(int id, string nombre, int duracionMin, decimal precio)
    {
        Id = id;
        Nombre = nombre;
        DuracionMin = duracionMin;
        Precio = precio;
    }

    public static ServicioItem Desde(Servicio s) =>
        new(s.Id, s.Nombre, s.DuracionMin, s.Precio);
}
