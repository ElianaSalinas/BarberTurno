namespace BarberTurno.Helpers;

/// <summary>
/// Animación de entrada para pantallas: los hijos directos del layout aparecen en cascada
/// (fade + desplazamiento suave). Uso en XAML:
///   xmlns:motion="clr-namespace:BarberTurno.Helpers"
///   &lt;VerticalStackLayout motion:Motion.Enter="True"&gt;
/// Es solo visual: si algo falla, los elementos quedan visibles y en su sitio.
/// </summary>
public static class Motion
{
    public static readonly BindableProperty EnterProperty =
        BindableProperty.CreateAttached("Enter", typeof(bool), typeof(Motion), false, propertyChanged: OnEnterChanged);

    public static bool GetEnter(BindableObject view) => (bool)view.GetValue(EnterProperty);
    public static void SetEnter(BindableObject view, bool value) => view.SetValue(EnterProperty, value);

    private static void OnEnterChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Layout layout || newValue is not true)
            return;

        EventHandler? handler = null;
        handler = async (_, _) =>
        {
            layout.Loaded -= handler;   // solo la primera vez que se muestra
            await RunAsync(layout);
        };
        layout.Loaded += handler;
    }

    private static async Task RunAsync(Layout layout)
    {
        var items = layout.Children
            .OfType<VisualElement>()
            .Where(v => v.IsVisible)
            .Take(14)
            .ToList();

        try
        {
            foreach (var v in items)
            {
                v.Opacity = 0;
                v.TranslationY = 22;
            }

            var tareas = new List<Task>();
            for (var i = 0; i < items.Count; i++)
                tareas.Add(AnimarAsync(items[i], i * 60));

            await Task.WhenAll(tareas);
        }
        catch
        {
            // Animación decorativa: ante cualquier fallo, se muestra todo normal.
        }
        finally
        {
            foreach (var v in items)
            {
                v.Opacity = 1;
                v.TranslationY = 0;
            }
        }
    }

    private static async Task AnimarAsync(VisualElement v, int retardoMs)
    {
        if (retardoMs > 0)
            await Task.Delay(retardoMs);

        await Task.WhenAll(
            v.FadeToAsync(1, 380, Easing.CubicOut),
            v.TranslateToAsync(0, 0, 420, Easing.CubicOut));
    }
}
