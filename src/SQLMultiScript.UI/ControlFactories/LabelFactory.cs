namespace SQLMultiScript.UI.ControlFactories
{
    public static class LabelFactory
    {
        public static Label Create(string text, DockStyle dock = DockStyle.Top)
        {
            var label = new Label
            {
                Text = text,
                Dock = dock
            };

            return label;
        }
    }
}
