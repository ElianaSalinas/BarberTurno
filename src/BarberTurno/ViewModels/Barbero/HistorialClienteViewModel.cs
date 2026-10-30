using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Models;
using BarberTurno.Services.Sync;

namespace BarberTurno.ViewModels;

/// <summary>Carga la lista de clientes para consultar el historial de sus citas.</summary>
public partial class HistorialClienteViewModel : BaseViewModel
{
    private readonly IClienteRepository _clientes;
    private readonly ISyncService _sync;

    public ObservableCollection<Cliente> ListaClientes { get; } = new();

    public HistorialClienteViewModel(IClienteRepository clientes, ISyncService sync)
    {
        _clientes = clientes;
        _sync = sync;
        Titulo = "Clientes";
    }

    public override void OnAppearing() => _ = CargarClientesAsync();

    [RelayCommand]
    /// <summary>Carga desde el repositorio los clientes que el barbero puede consultar.</summary>
    public async Task CargarClientesAsync()
    {
        State = ViewState.Cargando;
        MensajeError = null;

        try
        {
            await _sync.SincronizarCatalogosAsync();
            var items = await _clientes.GetAllAsync();
            ListaClientes.Clear();
            foreach (var c in items)
            {
                ListaClientes.Add(c);
            }

            State = ListaClientes.Count == 0 ? ViewState.Vacio : ViewState.ConDatos;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            MensajeError = "No se pudo cargar la lista de clientes.";
            State = ViewState.Error;
        }
    }
}
