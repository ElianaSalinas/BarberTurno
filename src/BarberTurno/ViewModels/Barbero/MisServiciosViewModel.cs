using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Services.Auth;
using BarberTurno.ViewModels;

namespace BarberTurno.ViewModels;

/// <summary>Panel del barbero: los servicios que ofrece, con la foto de cada uno.</summary>
public partial class MisServiciosViewModel : BaseViewModel
{
    private readonly IAuthService _auth;
    private readonly IServicioRepository _servicios;

    public ObservableCollection<ServicioItem> Servicios { get; } = new();

    [ObservableProperty]
    private bool sinServicios;

    public MisServiciosViewModel(IAuthService auth, IServicioRepository servicios)
    {
        _auth = auth;
        _servicios = servicios;
    }

    public override void OnAppearing() => _ = CargarAsync();

    [RelayCommand]
    private async Task CargarAsync()
    {
        try
        {
            var sesion = _auth.SesionActual;
            if (sesion is null)
            {
                State = ViewState.Error;
                MensajeError = "No hay una sesión activa.";
                return;
            }

            var barberoId = AgendaLoader.ObtenerBarberoId(sesion);
            if (barberoId is null)
            {
                State = ViewState.Error;
                MensajeError = "No se encontró tu perfil de barbero. Cierra sesión e inicia de nuevo.";
                return;
            }

            Servicios.Clear();
            var lista = await _servicios.GetByBarberoAsync(barberoId.Value);
            foreach (var s in lista)
                Servicios.Add(ServicioItem.Desde(s));

            SinServicios = Servicios.Count == 0;
            State = ViewState.ConDatos;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            MensajeError = "No se pudieron cargar tus servicios.";
            State = ViewState.Error;
        }
    }

    [RelayCommand]
    private Task IrAPerfilAsync() => Shell.Current.GoToAsync("//barbero/perfilbarbero");
}
