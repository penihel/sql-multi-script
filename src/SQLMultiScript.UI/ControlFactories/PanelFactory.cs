namespace SQLMultiScript.UI.ControlFactories
{
    public static class PanelFactory
    {
        public static Panel Create(DockStyle dock = DockStyle.Fill, int? height = null)
        {
            var panel = new Panel
            {
                Dock = dock,
                Padding = new Padding(UIConstants.PanelPadding),
            };

            if (height != null)
            {
                panel.Height = height.Value;
            }

            return panel;
        }
    }
}
