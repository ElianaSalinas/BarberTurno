using System.Globalization;

namespace BarberTurno.Helpers.Converters;

/// <summary>
/// Convierte el texto guardado como foto en un ImageSource.
/// Acepta: URL http(s), imagen embebida "data:image/...;base64,..." o ruta de archivo local.
/// Devuelve null si está vacío (la vista muestra entonces la inicial / el marcador).
/// </summary>
public class FotoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var texto = value as string;
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        try
        {
            if (texto.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                var coma = texto.IndexOf(',');
                if (coma < 0) return null;
                var bytes = System.Convert.FromBase64String(texto[(coma + 1)..]);
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }

            if (texto.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                // Sin internet no se intenta descargar la foto: se muestra la inicial en su lugar.
                // (Una descarga fallida de imagen remota en Android puede cerrar la app en algunos equipos.)
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                    return null;

                return new UriImageSource
                {
                    Uri = new Uri(texto),
                    CachingEnabled = true,
                    CacheValidity = TimeSpan.FromDays(7)
                };
            }

            return ImageSource.FromFile(texto);
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
