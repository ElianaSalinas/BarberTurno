namespace BarberTurno.Helpers;

/// <summary>Elegir una foto de la galería o tomarla con la cámara (con permiso) y devolverla reducida como "data:image/jpeg;base64,...".</summary>
public static class FotoHelper
{
    /// <summary>Elegir una foto de la galería (con permiso) y devolverla reducida.</summary>
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

    /// <summary>Tomar una foto con la cámara (con permiso) y devolverla reducida.</summary>
    /// <returns>La imagen embebida, o null si se canceló o se negó el permiso.</returns>
    public static async Task<(string? Foto, bool PermisoDenegado)> TomarAsync(int maxPx = 800)
    {
        var permiso = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (permiso != PermissionStatus.Granted)
            permiso = await Permissions.RequestAsync<Permissions.Camera>();
        if (permiso != PermissionStatus.Granted)
            return (null, true);

        if (!MediaPicker.Default.IsCaptureSupported)
            return (null, false);

        var archivo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions { Title = "Tomar foto" });
        if (archivo is null)
            return (null, false);

        await using var origen = await archivo.OpenReadAsync();
        using var imagen = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(origen);
        using var reducida = imagen.Downsize((float)maxPx, false);
        var bytes = reducida.AsBytes(Microsoft.Maui.Graphics.ImageFormat.Jpeg, 0.8f);
        return ("data:image/jpeg;base64," + Convert.ToBase64String(bytes), false);
    }
}
