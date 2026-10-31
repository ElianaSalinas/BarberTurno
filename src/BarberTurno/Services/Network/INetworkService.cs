// Responsable: M07
// Estado de la red (envoltorio de Connectivity).
namespace BarberTurno.Services.Network;

/// <summary>Expone el estado de red y sus cambios a servicios que trabajan sin conexión.</summary>
public interface INetworkService
{
    bool HayConexion { get; }
    event EventHandler<bool>? ConexionCambio;
}
