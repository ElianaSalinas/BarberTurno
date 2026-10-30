using Foundation;

namespace BarberTurno;

/// <summary>Delegado iOS que conecta el ciclo de vida nativo con la aplicación MAUI.</summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	/// <summary>Crea la aplicación usando el contenedor de dependencias común.</summary>
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
