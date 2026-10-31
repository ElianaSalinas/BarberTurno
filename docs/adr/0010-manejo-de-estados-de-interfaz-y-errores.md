# ADR-0010: Manejo de estados de interfaz y errores

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M09 (Albert Frank)

## Contexto
Toda pantalla que carga datos pasa por cuatro situaciones: cargando, sin datos, con error y con datos. Sin un criterio común, cada pantalla inventa sus banderas (`IsLoading`, `HasError`…), los errores de red llegan como excepciones y el usuario ve pantallas en blanco o aplicaciones que se cierran. La reserva, además, debe validarse antes de enviarse.

## Decisión
1. **Un enumerado único `ViewState`** (`Cargando`, `Vacio`, `Error`, `ConDatos`) en `BaseViewModel.State`, con `MensajeError` para el texto.
2. Dos controles de presentación reutilizables, sin lógica de negocio:
   - **`StateView`**: sustituye el contenido de la pantalla según el estado (listas de barberos, servicios, mis citas, hora, confirmar reserva, perfil), con botón **Reintentar** enlazado a un comando.
   - **`EstadoBanner`**: indicador compacto de carga/error para pantallas con cabecera y contenido propios (Mi agenda, Calendario, Inicio, Clientes, Mis servicios, Detalle de cita); vacío y con datos los resuelve la pantalla.
3. **Errores de red como valores, no como excepciones:** la capa de red devuelve `ApiResult<T>` (`Exito`, `CodigoHttp`, `SinConexion`, `Error`) y los ViewModels lo traducen a un estado y un mensaje en español.
4. **Sin conexión:** se muestra un aviso ("mostrando datos guardados") y se sigue leyendo de SQLite.
5. **Validación de la reserva** en `ConfirmarReservaViewModel.Validar()` antes de enviar (barbero, servicio y hora elegidos, y hora todavía en el futuro), seguida de una **revalidación** de que la hora sigue libre; un `409` del servidor se muestra como "hora ocupada" y no como error genérico.
6. Los ViewModels capturan las excepciones de sus cargas (`try/catch`) y pasan a `Error`; la aplicación nunca se cierra por un fallo de red.

## Alternativas consideradas
- **Banderas booleanas por pantalla:** simples al inicio, pero combinaciones inválidas (cargando y error a la vez) y duplicación en cada ViewModel.
- **Propagar excepciones hasta la interfaz con `try/catch` en cada vista:** dispersa el manejo de errores y complica los mensajes.
- **Ventanas emergentes (`DisplayAlert`) para todo:** interrumpen al usuario y no permiten reintentar desde la propia pantalla.

## Consecuencias
- **Ventajas:** comportamiento coherente en toda la app, ViewModels más pequeños y fáciles de probar (se comprueba `State`), reintento en un toque.
- **Costos:** dos controles con propósitos parecidos; el equipo debe elegir bien (¿la pantalla se reemplaza o tiene contenido propio?).
- **Riesgos y mitigación:** una carga que olvide poner `State = ConDatos` deja el indicador girando; las pruebas de ViewModel verifican el estado final de cada carga.
