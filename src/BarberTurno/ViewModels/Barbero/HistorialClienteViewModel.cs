using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Models;
using BarberTurno.Services.Sync;

namespace BarberTurno.ViewModels;

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
    public async Task CargarClientesAsync()
    {
        try
        {
            await _sync.SincronizarCatalogosAsync();
            var items = await _clientes.GetAllAsync();
            ListaClientes.Clear();
            foreach (var c in items)
            {
                ListaClientes.Add(c);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}
