using BarberTurno.Helpers;
using BarberTurno.Models;

namespace BarberTurno.ViewModels;

public sealed class ServicioItem
{
    public int Id { get; }
    public string Nombre { get; }
    public int DuracionMin { get; }
    public decimal Precio { get; }
    public string? FotoUrl { get; }

    public string DuracionTexto => $"{DuracionMin} min";
    public string PrecioTexto => FormatoCliente.Precio(Precio);
    public string Detalle => $"{FormatoCliente.Precio(Precio)} · {DuracionMin} min";

    /// <summary>
    /// Nombre del archivo de la foto del servicio, por convención a partir del nombre:
    /// "Corte clásico" -> servicio_corte_clasico.jpg (colócala en Resources/Images).
    /// Si el archivo no existe, la tarjeta muestra el marcador de tijeras.
    /// </summary>
    public string ImagenArchivo => "servicio_" + Slug(Nombre) + ".jpg";

    private static string Slug(string texto)
    {
        var normal = texto.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normal)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        }
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "_+", "_").Trim('_');
    }

    public ServicioItem(int id, string nombre, int duracionMin, decimal precio, string? fotoUrl = null)
    {
        FotoUrl = fotoUrl;
        Id = id;
        Nombre = nombre;
        DuracionMin = duracionMin;
        Precio = precio;
    }

    public static ServicioItem Desde(Servicio s) =>
        new(s.Id, s.Nombre, s.DuracionMin, s.Precio, s.FotoUrl);
}
