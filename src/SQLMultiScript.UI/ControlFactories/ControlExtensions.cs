namespace SQLMultiScript.UI.ControlFactories
{
    public static class ControlExtensions
    {
        public static T Customize<T>(this T control, Action<T> configure)
            where T : Control
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            configure(control);

            return control;
        }

        public static Control CenterVertically(this Control child)
        {
            var parent = child.Parent;
            if (parent == null || child == null) return child;

            int newTop = Math.Max(0, (parent.ClientSize.Height - child.Height) / 2);
            child.Top = newTop;

            return child;
        }
        public static Control CenterHorizontally(this Control child)
        {
            var parent = child?.Parent;
            if (parent == null || child == null)
                return child;

            int newLeft = Math.Max(0, (parent.ClientSize.Width - child.Width) / 2);
            child.Left = newLeft;

            return child;
        }

        public static Control CenterHorizontallyAndVertically(this Control child)
        {
            return child
                .CenterVertically()
                .CenterHorizontally();
        }
        public static Control StretchToNextSibling(this Control control)
        {
            if (control?.Parent == null)
                return control;

            var parent = control.Parent;

            // Ordena irmãos por posição X (da esquerda pra direita)
            var siblings = parent.Controls
                .Cast<Control>()
                .Where(c => c != control)
                .OrderBy(c => c.Left)
                .ToList();

            // Acha o próximo irmão à direita
            var nextSibling = siblings.FirstOrDefault(c => c.Left > control.Left);

            // Considera padding do pai no cálculo
            int leftEdge = parent.Padding.Left;
            int rightEdge;

            if (nextSibling != null)
            {
                rightEdge = nextSibling.Left - nextSibling.Margin.Left;
            }
            else
            {
                // Se não tiver irmão à direita, estica até o final do parent
                rightEdge = parent.ClientSize.Width - parent.Padding.Right;
            }

            // Calcula nova largura
            int newWidth = rightEdge - control.Left - control.Margin.Right;

            // Garante que não seja negativa
            control.Width = Math.Max(0, newWidth);

            return control;
        }


        public static Control AlignAndStretch(this Control control)
        {
            return control
                .CenterVertically()
                .StretchToNextSibling();
        }



    }

}
