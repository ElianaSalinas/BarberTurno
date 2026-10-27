# Reparto de responsabilidades (resumen)

| Id | Área | Carpetas principales | ADR |
|---|---|---|---|
| M01 | Arquitectura, Shell, DI, integración, Git | App.xaml, AppShell, MauiProgram, Helpers, Services/Navigation, Services/Dialogs, BaseViewModel | 0001, 0002, 0007 |
| M02 | Modelos y SQLite | Models/, Data/ | 0003 |
| M03 | API: base, auth, hosting | src/BarberTurno.Api (Program, Auth, Barberos, Servicios, Domain, Data) | 0012 |
| M04 | API: disponibilidad y reservas (concurrencia) | Citas, Disponibilidad, Services (API) | 0008 (servidor) |
| M05 | Red del cliente, DTOs, traductor | Services/Api, Dtos/, Mappers/ | 0005 |
| M06 | Autenticación, SecureStorage, notificaciones | Services/Auth, Services/Notifications, Login | 0006, 0009 |
| M07 | Sin conexión y sincronización | Services/Network, Services/Sync | 0004 |
| M08 | Disponibilidad y reserva (cliente) | Services/Availability, Services/Booking, SeleccionHora, ConfirmarReserva | 0008 (cliente) |
| M09 | UI del cliente, estados de pantalla | Views/Cliente, Perfil, Controls/StateView, Converters | 0010 |
| M10 | UI del barbero, tema claro/oscuro | Views/Barbero, Resources/Styles | 0011 |

Detalle completo en el PDF "Reparto de responsabilidades".
