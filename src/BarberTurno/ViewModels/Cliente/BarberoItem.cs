namespace BarberTurno.ViewModels;

/// <summary>Datos resumidos de un barbero para las listas de la interfaz.</summary>
public sealed class BarberoItem
{
    public int Id { get; }
    public string Nombre { get; }
    public string? FotoUrl { get; }
    public string ServiciosTexto { get; }

    public string Inicial => !string.IsNullOrWhiteSpace(Nombre)
        ? Nombre.Trim()[0].ToString().ToUpperInvariant()
        : "?";

    public bool TieneFoto => !string.IsNullOrWhiteSpace(FotoUrl);

    public BarberoItem(int id, string nombre, string? fotoUrl, string serviciosTexto)
    {
        Id = id;
        Nombre = nombre;
        FotoUrl = fotoUrl;
        ServiciosTexto = serviciosTexto;
    }
}
