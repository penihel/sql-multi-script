using Microsoft.Data.SqlClient;
using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;

namespace SQLMultiScript.UI.Forms
{
    public class NewConnectionForm : Form
    {
        private Label lblName, lblServer, lblAuthentication, lblUsername, lblPassword;
        private TextBox txtName, txtServer, txtUsername, txtPassword;
        private ComboBox cmbAuthentication;
        private Button btnTest, btnSave, btnCancel;



        private readonly IConnectionService _connectionService;
        private readonly BindingSource _bindingSource;
        private readonly Connection _connection;

        public NewConnectionForm(IConnectionService connectionService)
        {
            _connectionService = connectionService;

            // Instancia o modelo e o BindingSource
            _connection = new Connection();

            _bindingSource = new BindingSource { DataSource = _connection };

            InitializeForm();
            InitializeBindings();
        }
        private void InitializeBindings()
        {
            // Faz o binding dos campos com as propriedades do modelo
            txtName.DataBindings.Add(nameof(txtName.Text), _bindingSource, nameof(Connection.Name), false, DataSourceUpdateMode.OnPropertyChanged);
            txtServer.DataBindings.Add(nameof(txtServer.Text), _bindingSource, nameof(Connection.Server), false, DataSourceUpdateMode.OnPropertyChanged);
            cmbAuthentication.DataBindings.Add(nameof(cmbAuthentication.SelectedItem), _bindingSource, nameof(Connection.Auth), false, DataSourceUpdateMode.OnPropertyChanged);
            txtUsername.DataBindings.Add(nameof(txtUsername.Text), _bindingSource, nameof(Connection.UserName), false, DataSourceUpdateMode.OnPropertyChanged);
            txtPassword.DataBindings.Add(nameof(txtPassword.Text), _bindingSource, nameof(Connection.Password), false, DataSourceUpdateMode.OnPropertyChanged);
        }

        private void InitializeForm()
        {
            var screenSize = Screen.PrimaryScreen.WorkingArea;

            // Define tamanho como 70% da tela
            int width = (int)(screenSize.Width * 0.3);
            int height = (int)(screenSize.Height * 0.3);
            Size = new Size(width, height);
            ShowInTaskbar = false;

            Text = "Nova Conexão";

            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            // TableLayoutPanel principal
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = UIConstants.PanelPadding,
                ColumnCount = 2,
                RowCount = 6,
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            for (int i = 0; i < 5; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // última linha para botões

            Controls.Add(layout);


            // Labels e campos
            lblName = new Label { Text = "Nome:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtName = new TextBox { Dock = DockStyle.Fill };


            lblServer = new Label { Text = "Servidor:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtServer = new TextBox { Dock = DockStyle.Fill };

            lblAuthentication = new Label { Text = "Autenticação:", AutoSize = true, Anchor = AnchorStyles.Left };
            cmbAuthentication = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAuthentication.Items.AddRange(new string[]
            {
                Constants.WindowsAuthentication,
                Constants.SQLServerAuthentication,
                Constants.MicrosoftEntraMFA,
                Constants.MicrosoftEntraIntegrated,
                Constants.MicrosoftEntraPassword
            });
            cmbAuthentication.SelectedIndex = 0;
            cmbAuthentication.SelectedIndexChanged += CmbAuthentication_SelectedIndexChanged;

            lblUsername = new Label { Text = "Usuário:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtUsername = new TextBox { Dock = DockStyle.Fill };

            lblPassword = new Label { Text = "Senha:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };


            layout.Controls.Add(lblName, 0, 0);
            layout.Controls.Add(txtName, 1, 0);

            layout.Controls.Add(lblServer, 0, 1);
            layout.Controls.Add(txtServer, 1, 1);

            layout.Controls.Add(lblAuthentication, 0, 2);
            layout.Controls.Add(cmbAuthentication, 1, 2);

            layout.Controls.Add(lblUsername, 0, 3);
            layout.Controls.Add(txtUsername, 1, 3);

            layout.Controls.Add(lblPassword, 0, 4);
            layout.Controls.Add(txtPassword, 1, 4);

            // FlowLayoutPanel para botões
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                Padding = UIConstants.PanelPadding,
                AutoSize = true,
            };

            btnCancel = new Button
            {
                Width = 50,
                Image = Images.ic_fluent_dismiss_24_regular,
                Height = 50,
            };
            var toolTipBtnCancel = new ToolTip();
            toolTipBtnCancel.SetToolTip(btnCancel, Resources.Strings.Cancel);

            btnSave = new Button
            {
                Image = Images.ic_fluent_save_24_regular,
                Dock = DockStyle.Right,
                Width = 50
            };

            var toolTipBtnSave = new ToolTip();
            toolTipBtnSave.SetToolTip(btnSave, Resources.Strings.Save);


            btnTest = new Button
            {
                Image = Images.ic_fluent_database_plug_connected_20_regular,
                Dock = DockStyle.Right,
                Width = 50
            };

            var toolTipBtnTest = new ToolTip();
            toolTipBtnTest.SetToolTip(btnTest, Resources.Strings.TestConnection);




            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            btnSave.Click += BtnSave_Click;
            btnTest.Click += BtnTest_Click;

            buttonPanel.Controls.AddRange(new Button[] { btnSave, btnTest, btnCancel });

            layout.Controls.Add(buttonPanel, 0, 5);
            layout.SetColumnSpan(buttonPanel, 2);

            UpdateAuthenticationFields();
        }

        private void CmbAuthentication_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateAuthenticationFields();
        }

        private void UpdateAuthenticationFields()
        {
            var auth = cmbAuthentication.SelectedItem.ToString();
            txtUsername.Enabled = false;
            txtPassword.Enabled = false;

            switch (auth)
            {
                case Constants.SQLServerAuthentication:
                case Constants.MicrosoftEntraPassword:
                    txtUsername.Enabled = true;
                    txtPassword.Enabled = true;
                    break;
                case Constants.MicrosoftEntraMFA:
                    txtUsername.Enabled = true;
                    txtPassword.Enabled = false;
                    break;
            }
        }

        private async void BtnTest_Click(object sender, EventArgs e)
        {

            try
            {
                await _connectionService.TestAsync(_connection);

                MessageBox.Show("Conexão bem sucedida!", "Teste", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha na conexão:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {




            await _connectionService.SaveAsync(_connection);

            DialogResult = DialogResult.OK;
        }


    }
}
