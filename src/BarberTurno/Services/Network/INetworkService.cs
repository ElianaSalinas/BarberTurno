// Responsable: M07
// Estado de la red (envoltorio de Connectivity).
namespace BarberTurno.Services.Network;

public interface INetworkService
{
    bool HayConexion { get; }
    event EventHandler<bool>? ConexionCambio;
}

