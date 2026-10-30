# ADR-0002: Estrategia de navegación entre los perfiles de cliente y barbero

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M01 (Eliana)

## Contexto
La misma aplicación sirve a dos perfiles con pantallas distintas: el cliente (Inicio, Reservar, Mis turnos, Servicios, Perfil) y el barbero (Mi agenda, Calendario, Servicios, Clientes, Perfil). Además hay un flujo de inicio de sesión previo y pantallas apiladas (elegir servicio → hora → confirmar). Había que decidir cómo se separan los perfiles, cómo se llega a cada uno y cómo se evita que un perfil vea las pantallas del otro.

## Decisión
1. Se usa **Shell** con **tres `TabBar` raíz**: `auth` (login, sin barra de pestañas), `cliente` y `barbero`. Cada `ShellContent` crea su página con `ContentTemplate`, así que la página se resuelve desde el contenedor de dependencias.
2. Tras el login, `LoginViewModel` navega con rutas absolutas según el rol (`//cliente`, `//barbero`) y el cierre de sesión vuelve a `//login`. `NavigationService.NormalizarRuta` traduce esos atajos a la ruta canónica (`//cliente/inicio`, `//barbero/agenda`, `//auth/login`).
3. Las pantallas que se apilan (servicios, selección de hora, confirmar reserva, registro, detalle de cita) se registran en `AppShell.xaml.cs` con **constantes** de `Routes`, sin cadenas sueltas.
4. Toda navegación pasa por `INavigationService`, que descarta dobles toques con un semáforo y aplica un tiempo máximo de 5 s para no dejar la interfaz bloqueada. Los datos entre pantallas viajan como parámetros de Shell (`IQueryAttributable`, p. ej. `citaId`).
5. El arranque muestra primero una pantalla de presentación animada y luego sustituye la página de la ventana por `AppShell`.

## Alternativas consideradas
- **Dos `AppShell` distintos (uno por rol) reemplazados en la ventana:** aísla del todo los perfiles, pero duplica la configuración (colores, rutas) y complica las transiciones; descartado.
- **`NavigationPage` con `PushAsync` manual:** control total, pero hay que implementar a mano las pestañas, el botón atrás y el estado por pestaña; Shell ya los trae.
- **Flyout (menú lateral):** menos natural en móvil para 4-5 destinos de uso frecuente que las pestañas inferiores.

## Consecuencias
- **Ventajas:** una sola fuente de rutas, pestañas nativas por perfil, navegación testeable (se simula `INavigationService`).
- **Costos:** los dos perfiles viven en el mismo Shell; las páginas del otro perfil existen como definición aunque no se creen hasta usarse. La separación depende de **no navegar** a rutas del otro rol, no de un bloqueo técnico (la API sí protege los datos por rol/token).
- **Riesgos y mitigación:** las rutas relativas dependen de la pestaña actual; por eso las pantallas apiladas se abren con nombres registrados y las raíces con rutas absolutas normalizadas.
