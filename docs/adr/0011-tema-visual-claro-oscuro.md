# ADR-0011: Tema visual claro y oscuro

- **Estado:** Aceptado
- **Fecha:** 2026-10-30
- **Responsable(s):** M10 (Equipo)

## Contexto
La práctica exige soporte de tema claro y oscuro y una interfaz cuidada. La marca de la barbería pide una paleta sobria (carbón, crema y dorado). Había que decidir cómo se definen los colores una sola vez, cómo cambia el tema y si la elección del usuario se recuerda.

## Decisión
1. Los colores son **tokens con nombre** en `Resources/Styles` (`M10Light*` y `M10Dark*` para página, superficie, texto, borde, tarjeta y dorado). Las vistas no escriben colores sueltos salvo contadas excepciones.
2. Cada token se usa con **`AppThemeBinding`** (`Light=…, Dark=…`), así la interfaz reacciona al instante cuando cambia el tema.
3. Estilos reutilizables (`BtCard`, `BtDarkCard`, `BtField`, `BtGoldButton`, `BtTitle`, …) concentran tipografía (Open Sans), radios y espacios.
4. El tema lo elige el usuario con un interruptor en **Perfil**; se guarda en `Preferences` (`tema_oscuro`) y `App` lo aplica al iniciar (`UserAppTheme`).
5. Los íconos son SVG; los que no se leen sobre fondo oscuro tienen variante (`icon_bell` / `icon_bell_dark`) elegida con `AppThemeBinding`. La barra de estado de Android toma el color de fondo de la app.

## Alternativas consideradas
- **Seguir solo el tema del sistema:** menos código, pero el usuario no puede elegir; la práctica pide soporte explícito de ambos temas.
- **Intercambiar `ResourceDictionary` en tiempo de ejecución con `DynamicResource`:** flexible, pero reemplazar diccionarios completos es más frágil y propenso a olvidos que `AppThemeBinding` por propiedad.
- **Librería de componentes Material:** acelera, pero impone su estética y añade una dependencia grande.

## Consecuencias
- **Ventajas:** los colores se cambian en un solo lugar, ambos temas siempre coherentes, preferencia recordada entre sesiones.
- **Costos:** `AppThemeBinding` es verboso en XAML; cada color nuevo exige definir la pareja claro/oscuro.
- **Riesgos y mitigación:** quedan algunos colores fijos en vistas antiguas y los SVG monocromos necesitan su variante oscura a mano. Se revisan en cada pantalla nueva al probar en ambos temas.
