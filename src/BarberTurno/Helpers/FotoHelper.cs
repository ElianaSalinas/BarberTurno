namespace BarberTurno.Helpers;

/// <summary>Elegir una foto de la galería (con permiso) y devolverla reducida como "data:image/jpeg;base64,...".</summary>
public static class FotoHelper
{
    /// <returns>La imagen embebida, o null si se canceló o se negó el permiso.</returns>
    public static async Task<(string? Foto, bool PermisoDenegado)> ElegirAsync(int maxPx = 800)
    {
        var permiso = await Permissions.CheckStatusAsync<Permissions.Photos>();
        if (permiso != PermissionStatus.Granted)
            permiso = await Permissions.RequestAsync<Permissions.Photos>();
        if (permiso != PermissionStatus.Granted)
            return (null, true);

        var resultados = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { Title = "Selecciona una foto" });
        var archivo = resultados?.FirstOrDefault();
        if (archivo is null)
            return (null, false);

        await using var origen = await archivo.OpenReadAsync();
        using var imagen = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(origen);
        using var reducida = imagen.Downsize((float)maxPx, false);
        var bytes = reducida.AsBytes(Microsoft.Maui.Graphics.ImageFormat.Jpeg, 0.8f);
        return ("data:image/jpeg;base64," + Convert.ToBase64String(bytes), false);
    }
}
