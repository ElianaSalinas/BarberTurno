using Android.App;
using Android.Runtime;

namespace BarberTurno;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			var ex = args.ExceptionObject as Exception;
			var texto = $"[CRASH AppDomain {DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n\n";
			Android.Util.Log.Error("BarberTurnoCrash", texto);
			try
			{
				var ruta = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "crash.txt");
				System.IO.File.AppendAllText(ruta, texto);
			}
			catch { }
		};

		Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
		{
			var texto = $"[CRASH AndroidEnvironment {DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{args.Exception}\n\n";
			Android.Util.Log.Error("BarberTurnoCrash", texto);
			try
			{
				var ruta = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "crash.txt");
				System.IO.File.AppendAllText(ruta, texto);
			}
			catch { }
		};
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
