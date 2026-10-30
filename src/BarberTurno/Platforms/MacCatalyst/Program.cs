using ObjCRuntime;
using UIKit;

namespace BarberTurno;

/// <summary>Punto de entrada Mac Catalyst que delega el inicio nativo a <see cref="AppDelegate"/>.</summary>
public class Program
{
	/// <summary>Arranca UIKit y enlaza el ciclo de vida de Mac Catalyst con MAUI.</summary>
	static void Main(string[] args)
	{
		// AppDelegate conecta UIKit con el contenedor definido en MauiProgram.
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
