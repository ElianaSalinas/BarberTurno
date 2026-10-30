namespace BarberTurno.Helpers;

public static class AppConstants
{
    // URL de la API desplegada en Render (debe terminar en /api/)
    private const string UrlProduccion = "https://barberturno.onrender.com/api/";

    public static string ApiBaseUrl
    {
        get
        {
#if ANDROID
            // Solo el emulador usa la API local de la PC; un móvil real siempre usa la nube.
            if (DeviceInfo.Current.DeviceType == DeviceType.Virtual)
                return "http://10.0.2.2:5000/api/";
#endif
            return UrlProduccion;
        }
    }
}