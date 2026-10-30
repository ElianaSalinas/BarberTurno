using BarberTurno.Helpers;
using BarberTurno.Models;

namespace BarberTurno.ViewModels;

public sealed class ServicioItem
{
    public int Id { get; }
    public string Nombre { get; }
    public int DuracionMin { get; }
    public decimal Precio { get; }

    public string DuracionTexto => $"{DuracionMin} min";
    public string PrecioTexto => FormatoCliente.Precio(Precio);
    public string Detalle => $"{FormatoCliente.Precio(Precio)} · {DuracionMin} min";

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
