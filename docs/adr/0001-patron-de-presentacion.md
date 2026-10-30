# ADR-0001: Patrón de presentación (MVVM) y su justificación

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M01 (Eliana)

## Contexto
BarberTurno tiene unas 15 pantallas con estados de carga/vacío/error, formularios con validación y datos que llegan de SQLite y de una API. En .NET MAUI el enlace de datos de XAML es la forma natural de conectar la interfaz con la lógica. La práctica exige MVVM con las herramientas de la comunidad, inyección de dependencias y **ausencia de lógica de negocio en el code-behind**. Queríamos que la lógica de cada pantalla se pudiera probar sin levantar la interfaz (el PDF pide al menos una prueba de ViewModel con un repositorio simulado).

## Decisión
1. Se usa **MVVM** con `CommunityToolkit.Mvvm` 8.4: `[ObservableProperty]` y `[RelayCommand]` generan propiedades y comandos en tiempo de compilación.
2. Todos los ViewModels heredan de `BaseViewModel` (`State`, `MensajeError`, `IsBusy`, `OnAppearing/OnDisappearing`), de modo que cada pantalla expone el mismo vocabulario de estados.
3. Las vistas declaran `x:DataType` (enlaces compilados) y el proyecto usa `MauiXamlInflator=SourceGen`: los errores de enlace se detectan al compilar.
4. El **code-behind solo** asigna el `BindingContext` y reenvía `OnAppearing` al ViewModel. Las dependencias llegan por constructor (ver ADR-0007).
5. Capas: `Models` → `Data` (repositorios) → `Services` (API, sincronización, reserva, notificaciones) → `ViewModels` → `Views`. Un ViewModel nunca conoce a una vista.

## Alternativas consideradas
- **Code-behind con eventos (sin patrón):** rápido para una pantalla, pero la lógica queda atada a controles y no se puede probar; descartado por el requisito de la práctica y porque triplica el trabajo al cambiar el diseño.
- **MVU / Comet / MauiReactor:** modelo unidireccional atractivo, pero sale del conjunto XAML + herramientas de la comunidad que pide el curso y el equipo no lo conocía.
- **ReactiveUI / Prism:** más potentes (enrutamiento, regiones), pero añaden dependencias y una curva de aprendizaje que no se justifica para 15 pantallas.

## Consecuencias
- **Ventajas:** lógica probada con Moq sin interfaz (`ViewModelMockTests`), pantallas coherentes, menos código repetitivo gracias a los generadores.
- **Costos:** los generadores esconden código (hay que saber que `CargarAsync` produce `CargarCommand`); los enlaces compilados obligan a declarar `x:DataType` en cada plantilla.
- **Riesgos y mitigación:** la disciplina "sin lógica en code-behind" depende del equipo. Se revisa en cada integración. Algunos manejadores de toque (campana de avisos, "Ver calendario") viven en el code-behind pero solo delegan en un ayudante (`AvisosHelper`) o navegan; si crecen, deben pasar a un comando.
