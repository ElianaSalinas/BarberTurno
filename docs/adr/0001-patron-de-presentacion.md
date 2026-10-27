# ADR-0001: Patrón de presentación (MVVM)

- **Estado:** Aceptado
- **Fecha:** 2026-10-27
- **Responsable(s):** M01

## Contexto
BarberTurno tiene unas diez pantallas para dos perfiles (cliente y barbero), y diez integrantes trabajan en paralelo.
Necesitamos separar la interfaz (XAML) de la lógica de pantalla y de las reglas de negocio, para que:
- cada integrante pueda trabajar en su capa sin pisar archivos de otros,
- la lógica de las pantallas se pueda probar sin abrir la interfaz,
- todas las pantallas manejen igual la carga, los datos vacíos y los errores (ADR-0010).

.NET MAUI está pensado para trabajar con *data binding* en XAML, pero no impone ningún patrón.

## Decisión
Usamos **MVVM con CommunityToolkit.Mvvm**:
- **View** (`Views/`): solo XAML y un code-behind mínimo, que recibe su ViewModel por constructor y lo asigna a `BindingContext`.
- **ViewModel** (`ViewModels/`): hereda de `BaseViewModel` (`ObservableObject`). Usa `[ObservableProperty]` y `[RelayCommand]`
  (código generado, sin `INotifyPropertyChanged` escrito a mano). No contiene reglas de negocio: delega en los servicios
  (`Services/`) y en los repositorios (`Data/`).
- **Model** (`Models/`): entidades y resultados del dominio, sin dependencias de la interfaz.
- `BaseViewModel` centraliza `State` (Cargando / ConDatos / Vacio / Error), `MensajeError`, `Titulo` e `IsBusy`. También
  tiene los métodos `CargarAsync` y `EjecutarAsync`, que traducen las excepciones a mensajes para el usuario
  (`TraducirError`, sobrescribible).
- Los ViewModels navegan y muestran diálogos a través de `INavigationService` e `IDialogService`, nunca con
  `Shell.Current` ni `DisplayAlert` directamente.

## Alternativas consideradas
- **Elegida: MVVM + CommunityToolkit.Mvvm.** Es el patrón para el que está pensado el binding de MAUI. El toolkit es
  oficial (.NET Foundation) y elimina el código repetitivo con *source generators*, sin usar reflexión en tiempo de
  ejecución, así que tampoco cuesta rendimiento.
- **Code-behind (lógica en `*.xaml.cs`):** es lo más rápido para empezar, pero mezcla la interfaz con la lógica, no se
  puede probar sin la interfaz y provocaría conflictos de Git entre los módulos 7, 9 y 10, que tocarían los mismos archivos.
- **MVVM "a mano" o con un framework más pesado (Prism, ReactiveUI):** escribir `INotifyPropertyChanged` y `ICommand`
  a mano funciona, pero es repetitivo y fácil de equivocar. Prism y ReactiveUI añaden módulos, observables y
  convenciones que el equipo no necesita para una app de este tamaño, y alargarían el aprendizaje.
- **MVU (Comet / C# Markup):** obliga a escribir la interfaz en C# en lugar de XAML. Tiene poco soporte oficial en MAUI
  y casi no hay material de estudio.

## Consecuencias
- **Ventajas:** separación clara por carpetas, que coincide con el reparto de responsabilidades. Los ViewModels se pueden
  probar con dobles de los servicios. Todas las pantallas manejan los estados y errores de la misma forma gracias a
  `BaseViewModel`.
- **Costos:** hay más archivos por pantalla (View, ViewModel y registro en la inyección de dependencias). El equipo
  tiene que aprender el binding (`x:DataType`, comandos) y los atributos del toolkit.
- **Riesgos y mitigación:**
  - *Lógica de negocio dentro del ViewModel.* Lo revisamos en cada Pull Request, y cada ViewModel lo recuerda con su
    comentario `TODO`.
  - *Errores de binding silenciosos.* Usamos bindings compilados (`x:DataType`) y revisamos la salida de depuración.
  - *Doble ejecución de comandos.* `CargarAsync` y `EjecutarAsync` no hacen nada si `IsBusy` es verdadero.
