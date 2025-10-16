namespace SQLMultiScript.UI.ControlFactories
{
    public static class PanelFactory
    {
        public static Panel Create(int? height = null, DockStyle dock = DockStyle.Fill)
        {
            var panel = new Panel
            {
                Dock = dock,
                Padding = UIConstants.PanelPadding,
            };

            if (height != null)
            {
                panel.Height = height.Value;
            }

            return panel;
        }

        public static FlowLayoutPanel CreateFlowLayoutPanel(FlowDirection flowDirection, DockStyle dock = DockStyle.Fill, int? height = null)
        {
            var flowLayoutPanel = new FlowLayoutPanel
            {
                Dock = dock,
                FlowDirection = flowDirection,
                Padding = UIConstants.PanelPadding
            };

            if (height != null)
            {
                flowLayoutPanel.Height = height.Value;
            }

            return flowLayoutPanel;
        }

        public static Panel CreateDivider()
        {
            // Divider
            var divider = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 1,
                BackColor = Color.LightGray,
                Margin = UIConstants.DividerMargin,
            };



            return divider;
        }



    }
}
