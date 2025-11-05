namespace SQLMultiScript.UI.UserControls
{
    
    public class OutputMessagesPanel : UserControl
    {
        private readonly RichTextBox rtb;

        public OutputMessagesPanel()
        {
            rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BackColor = Color.White,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                DetectUrls = false,
                WordWrap = false
            };
            
            Dock = DockStyle.Fill;

            Controls.Add(rtb);
        }

        public void ClearMessages() => rtb.Clear();

        public void AppendInfo(string message)
            => AppendColored(message, Color.Black);

        public void AppendSuccess(string message)
            => AppendColored(message, Color.FromArgb(0, 128, 0)); // green

        public void AppendWarning(string message)
            => AppendColored(message, Color.FromArgb(255, 128, 0)); // orange

        public void AppendError(string message)
            => AppendColored(message, Color.Red);

        public void AppendSeparator()
        {
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionColor = Color.Gray;
            rtb.AppendText(new string('-', 60) + Environment.NewLine);
            rtb.SelectionColor = rtb.ForeColor;
        }

        private void AppendColored(string message, Color color)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            

            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionColor = color;
            rtb.AppendText(message.TrimEnd() + Environment.NewLine);
            rtb.SelectionColor = rtb.ForeColor;
            rtb.ScrollToCaret();
        }
    }
}
