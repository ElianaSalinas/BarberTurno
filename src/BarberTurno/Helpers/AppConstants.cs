namespace BarberTurno.Helpers;

public static class AppConstants
{
    // Pon aquí la URL que te dé Render al desplegar (debe terminar en /api/).
    private const string UrlProduccion = "https://TU-API.onrender.com/api/";

    public static string ApiBaseUrl
    {
        get
        {
#if DEBUG
#if ANDROID
            return "http://10.0.2.2:5000/api/";   // emulador Android
#else
            return "http://localhost:5000/api/";
#endif
#else
            return UrlProduccion;                  // Release: tu móvil usa la API en la nube
#endif
        }
    }
}