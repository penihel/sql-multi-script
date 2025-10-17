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

            if (nextSibling != null)
            {
                int rightEdge = nextSibling.Left;
                int newWidth = rightEdge - control.Left - control.Margin.Right;
                control.Width = Math.Max(0, newWidth);
            }
            else
            {
                // Se não tiver irmão à direita, estica até o final do parent
                int rightEdge = parent.ClientSize.Width - control.Margin.Right;
                int newWidth = rightEdge - control.Left;
                control.Width = Math.Max(0, newWidth);
            }

            return control;
        }


    }

}
