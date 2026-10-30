using ObjCRuntime;
using UIKit;

namespace BarberTurno;

/// <summary>Punto de entrada nativo que entrega el control de iOS a <see cref="AppDelegate"/>.</summary>
public class Program
{
	/// <summary>Inicia UIKit con el delegado que construye la aplicación MAUI.</summary>
	static void Main(string[] args)
	{
		// UIKit crea AppDelegate, que enlaza el ciclo de vida iOS con MauiProgram.
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
