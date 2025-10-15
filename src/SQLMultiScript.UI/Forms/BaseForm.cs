namespace SQLMultiScript.UI.Forms
{
    public abstract class BaseForm : Form
    {
        protected readonly ToolTip ToolTip = new ToolTip();


        protected void InitializeFormAsFixedDialog() 
        {
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            
        }

        protected void SetFormDialogSize(decimal percent) 
        {
            var screenSize = Screen.PrimaryScreen.WorkingArea;

            // Set size to 70% of the screen
            int width = (int)(screenSize.Width * percent);
            int height = (int)(screenSize.Height * percent);
            
            Size = new Size(width, height);
        }
    }
}
