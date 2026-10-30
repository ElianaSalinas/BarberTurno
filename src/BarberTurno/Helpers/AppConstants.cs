namespace BarberTurno.Helpers;

public static class AppConstants
{
    // Android emulador: http://10.0.2.2:5000/api/
    // Windows / iOS / MacCatalyst: http://localhost:5000/api/ o la URL pública HTTPS de producción.
    public static string ApiBaseUrl
    {
        get
        {
#if ANDROID
            return "http://10.0.2.2:5000/api/";
#else
            return "http://localhost:5000/api/";
#endif
        }
    }
}
