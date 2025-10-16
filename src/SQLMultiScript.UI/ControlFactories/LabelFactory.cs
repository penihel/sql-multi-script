namespace SQLMultiScript.UI.ControlFactories
{
    public static class LabelFactory
    {
        public static Label Create(string text, DockStyle dock = DockStyle.None)
        {
            var label = new Label
            {
                Text = text,
                Dock = dock,
                AutoSize = true,
            };

            

            return label;
        }
    }
}
