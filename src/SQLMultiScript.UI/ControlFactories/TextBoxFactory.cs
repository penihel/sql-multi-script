namespace SQLMultiScript.UI.ControlFactories
{
    public static class TextBoxFactory
    {
        public static TextBox CreateConsoleStyle()
        {
            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.Gainsboro,
                BorderStyle = BorderStyle.None
            };

            

            return textBox;
        }
    }
}
