# ADR-0002: Navegación entre perfiles de cliente y barbero

- **Estado:** Aceptado
- **Fecha:** 2026-10-27
- **Responsable(s):** M01

## Contexto
La app tiene dos perfiles, y cada uno ve pantallas distintas:
- **Cliente:** Barberos → Servicios → Selección de hora → Confirmar reserva; además, Mis citas y Perfil.
- **Barbero:** Agenda del día → Detalle de cita / Historial del cliente; además, Perfil.

Después del login, el usuario debe llegar a su perfil sin poder volver atrás al login ni entrar a las pantallas del
otro rol. Además, los ViewModels tienen que poder navegar y pasar parámetros sin depender de la interfaz (ADR-0001).

## Decisión
Usamos **.NET MAUI Shell** con **un TabBar por rol**:
- `AppShell.xaml` declara tres raíces: `login` (`ShellContent`), `cliente` (TabBar: Barberos, Mis citas, Perfil) y
  `barbero` (TabBar: Agenda, Perfil). El flyout está desactivado.
- Las pantallas apiladas (`servicios`, `seleccionhora`, `confirmarreserva`, `historialcliente`, `detallecita`) se
  registran con `Routing.RegisterRoute` en `AppShell.xaml.cs`.
- Todas las rutas son constantes en `Helpers/Routes.cs`, sin cadenas sueltas. Las raíces usan `//` (`Routes.Cliente`,
  `Routes.Barbero`, `Routes.Login`), lo que reemplaza la pila completa: después del login o del logout no se puede volver
  atrás.
- Los ViewModels navegan solo con `INavigationService` (`GoToAsync(ruta, parámetros)` y `GoBackAsync()`), que envuelve
  `Shell.Current.GoToAsync` y siempre se ejecuta en el hilo principal. Los parámetros llegan con `[QueryProperty]` o
  `IQueryAttributable`.
- Al iniciar la app (`App.xaml.cs`), si hay una sesión guardada (ADR-0006), se navega directamente a `//cliente` o a
  `//barbero` según el rol. Si no hay sesión, se queda en `//login`.

## Alternativas consideradas
- **Elegida: Shell con un TabBar por rol.** Es el sistema de navegación recomendado en MAUI. Permite navegar por rutas
  (útil para los ViewModels), reemplaza la pila con `//` y crea las páginas con la inyección de dependencias
  (`ContentTemplate`). Así, separar los roles se reduce a elegir la raíz.
- **NavigationPage + TabbedPage manual:** con `PushAsync` y `PopAsync` hay que manejar la pila a mano y armar las
  pestañas según el rol en código. Además, cada ViewModel necesitaría una referencia a `INavigation`, que está atado a la
  página.
- **Un solo TabBar que oculta pestañas según el rol:** es menos código, pero las rutas del otro rol seguirían existiendo
  y se podría llegar a ellas por error. Es más frágil que tener raíces separadas.
- **Dos apps o dos ventanas distintas:** duplica la configuración, la compilación y la publicación. No tiene sentido
  para dos perfiles que comparten modelos, servicios y login.

## Consecuencias
- **Ventajas:** el flujo es predecible (login → raíz del rol) y no se puede volver atrás al login. Los ViewModels quedan
  desacoplados de Shell, y se pueden probar con un `INavigationService` falso. Las rutas están en un solo archivo.
- **Costos:** los parámetros de navegación se pasan como `IDictionary<string, object>` y se reciben por nombre, así que
  el compilador no comprueba los tipos. Shell también tiene menos control fino de las animaciones y de la pila que
  `NavigationPage`.
- **Riesgos y mitigación:**
  - *Una ruta apilada sin registrar falla en tiempo de ejecución.* Cada página nueva debe agregarse a `Routes.cs` y a
    `AppShell.xaml.cs` en el mismo Pull Request.
  - *Nombres de parámetros mal escritos.* Definimos los nombres como constantes y los revisamos en el Pull Request.
  - *`PerfilPage` está en ambos TabBar.* Se crea una instancia distinta en cada uno (Transient, ADR-0007), así que no
    comparten estado.
