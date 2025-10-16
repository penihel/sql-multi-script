namespace SQLMultiScript.UI.ControlFactories
{
    public static class ButtonFactory
    {
        public static Button Create(
            ToolTip toolTip = null,
            string toolTipText = null,
            Image image = null,
            EventHandler onClick = null,
            DockStyle? dockStyle = null)
        {
            var button = new Button
            {
                Size = UIConstants.ButtonSize,                
            };

            if (image != null)
                button.Image = image;


            if (dockStyle.HasValue)
                button.Dock = dockStyle.Value;

            if (onClick != null)
                button.Click += onClick;

            if (toolTip != null && !string.IsNullOrEmpty(toolTipText))
                toolTip.SetToolTip(button, toolTipText);

            return button;
        }
    }
}
