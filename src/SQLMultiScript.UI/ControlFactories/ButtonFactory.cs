namespace SQLMultiScript.UI.ControlFactories
{
    public static class ButtonFactory
    {
        public static Button Create(
            ToolTip toolTip,
            string toolTipText, 
            Image image,
            DockStyle dockStyle = DockStyle.Right,
            EventHandler onClick = null)
        {
            var button = new Button
            {
                Image = image,
                Size = UIConstants.ButtonSize,
                Dock = dockStyle
            };

            if (onClick != null)
                button.Click += onClick;

            toolTip.SetToolTip(button, toolTipText);

            return button;
        }
    }
}
