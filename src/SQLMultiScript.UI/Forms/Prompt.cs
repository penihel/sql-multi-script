using SQLMultiScript.Resources;
using SQLMultiScript.UI;
using SQLMultiScript.UI.ControlFactories;

public static class Prompt
{
    public static string ShowDialog(string text, string caption)
    {
        Form prompt = new Form()
        {
            Width = 500,
            Height = 200,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = caption,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = true

        };


        // ToolTip compartilhado
        ToolTip toolTip = new ToolTip();

        // TableLayoutPanel principal
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = UIConstants.PanelPadding
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // TextBox
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // TextBox
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); // Rodapé

        




        // Label
        var textLabel = new Label
        {
            Text = text,
            AutoSize = true,
            Dock = DockStyle.Fill,

        };

        // TextBox
        var textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            AutoSize = true,

        };



        // Painel para divider + botões
        var bottomFlowLayoutPanel = PanelFactory
            .CreateFlowLayoutPanel(FlowDirection.RightToLeft);






        
        var btnOk = ButtonFactory.Create(
            toolTip,
            Strings.OK,
            Images.ic_fluent_checkmark_24_regular
        );
        

        btnOk.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                MessageBox.Show(prompt, Strings.FieldCannotBeEmpty, Strings.Attention, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox.Focus();
                return;
            }
            // Se passou na validação, fecha o form como OK
            prompt.DialogResult = DialogResult.OK;
            prompt.Close();
        };

        // Botão Cancel
        var btnCancel = ButtonFactory.Create(
            toolTip,
            Strings.Cancel,
            Images.ic_fluent_dismiss_24_regular
        );
        btnCancel.DialogResult = DialogResult.Cancel;

        


        
        bottomFlowLayoutPanel.Controls.Add(btnOk);
        bottomFlowLayoutPanel.Controls.Add(btnCancel);



        // Adiciona controles ao layout principal
        mainLayout.Controls.Add(textLabel, 0, 0);
        mainLayout.Controls.Add(textBox, 0, 1);
        mainLayout.Controls.Add(PanelFactory.CreateDivider(), 0, 2);
        mainLayout.Controls.Add(bottomFlowLayoutPanel, 0, 3);

        prompt.Controls.Add(mainLayout);

        prompt.AcceptButton = btnOk;
        prompt.CancelButton = btnCancel;

        return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : null;
    }
}