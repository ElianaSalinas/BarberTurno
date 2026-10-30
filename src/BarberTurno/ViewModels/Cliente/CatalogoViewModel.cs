using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Services.Navigation;

namespace BarberTurno.ViewModels;

/// <summary>Carga el catálogo local de servicios para su consulta desde la aplicación.</summary>
public partial class CatalogoViewModel : BaseViewModel
{
    private readonly IServicioRepository _servicios;
    private readonly INavigationService _nav;

    public CatalogoViewModel(IServicioRepository servicios, INavigationService nav)
    {
        _servicios = servicios; _nav = nav;
    }

    [ObservableProperty] private ServicioItem? destacado;
    public ObservableCollection<ServicioItem> Servicios { get; } = new();

    public override void OnAppearing() => _ = CargarAsync();

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (Servicios.Count == 0) State = ViewState.Cargando;
        try
        {
            var lista = (await _servicios.GetAllAsync()).OrderBy(s => s.Precio).Select(ServicioItem.Desde).ToList();
            Servicios.Clear();
            foreach (var s in lista) Servicios.Add(s);
            Destacado = lista.OrderByDescending(s => s.Precio).FirstOrDefault();
            State = lista.Count == 0 ? ViewState.Vacio : ViewState.ConDatos;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            MensajeError = "No pudimos cargar los servicios.";
            State = ViewState.Error;
        }
    }

    // Para reservar primero se elige barbero.
    [RelayCommand] private Task ReservarAsync() => _nav.GoToAsync("//cliente/barberos");
}
