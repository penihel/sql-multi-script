namespace SQLMultiScript.UI.ControlFactories
{
    public static class ButtonFactory
    {
        public static Button Create(
            ToolTip toolTip = null,
            string toolTipText = null,
            Image image = null,
            DockStyle? dockStyle = null,
            EventHandler onClick = null)
        {
            var button = new Button
            {
                Image = image,
                Size = UIConstants.ButtonSize,
                Margin = UIConstants.ButtonMargin
            };

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
