using Microsoft.Data.SqlClient;

namespace SQLMultiScript.UI
{
    public class NewConnectionForm : Form
    {
        private Label lblServer, lblAuthentication, lblUsername, lblPassword;
        private TextBox txtServer, txtUsername, txtPassword;
        private ComboBox cmbAuthentication;
        private Button btnTest, btnSave, btnCancel;

        public string ConnectionName { get; private set; }
        public string ConnectionString { get; private set; }

        public NewConnectionForm()
        {
            InitializeForm();
        }

        private void InitializeForm()
        {
            Text = "Nova Conexão";
            Size = new Size(450, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            // TableLayoutPanel principal
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ColumnCount = 2,
                RowCount = 5,
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            for (int i = 0; i < 4; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // última linha para botões

            Controls.Add(layout);

            // Labels e campos
            lblServer = new Label { Text = "Servidor:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtServer = new TextBox { Dock = DockStyle.Fill };

            lblAuthentication = new Label { Text = "Autenticação:", AutoSize = true, Anchor = AnchorStyles.Left };
            cmbAuthentication = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAuthentication.Items.AddRange(new string[]
            {
                "Windows Authentication",
                "SQL Server Authentication",
                "Microsoft Entra MFA",
                "Microsoft Entra Integrated",
                "Microsoft Entra Password"
            });
            cmbAuthentication.SelectedIndex = 0;
            cmbAuthentication.SelectedIndexChanged += CmbAuthentication_SelectedIndexChanged;

            lblUsername = new Label { Text = "Usuário:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtUsername = new TextBox { Dock = DockStyle.Fill };

            lblPassword = new Label { Text = "Senha:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };

            layout.Controls.Add(lblServer, 0, 0);
            layout.Controls.Add(txtServer, 1, 0);

            layout.Controls.Add(lblAuthentication, 0, 1);
            layout.Controls.Add(cmbAuthentication, 1, 1);

            layout.Controls.Add(lblUsername, 0, 2);
            layout.Controls.Add(txtUsername, 1, 2);

            layout.Controls.Add(lblPassword, 0, 3);
            layout.Controls.Add(txtPassword, 1, 3);

            // FlowLayoutPanel para botões
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                AutoSize = true
            };

            btnCancel = new Button { Text = "Cancelar", Width = 90 };
            btnSave = new Button { Text = "Salvar", Width = 90 };
            btnTest = new Button { Text = "Testar", Width = 90 };

            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            btnSave.Click += BtnSave_Click;
            btnTest.Click += BtnTest_Click;

            buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnSave, btnTest });

            layout.Controls.Add(buttonPanel, 0, 4);
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
                case "SQL Server Authentication":
                case "Microsoft Entra Password":
                    txtUsername.Enabled = true;
                    txtPassword.Enabled = true;
                    break;
                case "Microsoft Entra MFA":
                    txtUsername.Enabled = true;
                    txtPassword.Enabled = false;
                    break;
            }
        }

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            var connString = BuildConnectionString();
            try
            {
                using var conn = new SqlConnection(connString);
                await conn.OpenAsync();
                MessageBox.Show("Conexão bem sucedida!", "Teste", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha na conexão:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            ConnectionString = BuildConnectionString();
            ConnectionName = txtServer.Text;
            DialogResult = DialogResult.OK;
        }

        private string BuildConnectionString()
        {
            string server = txtServer.Text.Trim();
            string auth = cmbAuthentication.SelectedItem.ToString();

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = "master",
                ConnectTimeout = 5
            };

            switch (auth)
            {
                case "Windows Authentication":
                    builder.IntegratedSecurity = true;
                    break;
                case "SQL Server Authentication":
                    builder.UserID = txtUsername.Text;
                    builder.Password = txtPassword.Text;
                    builder.IntegratedSecurity = false;
                    break;
                case "Microsoft Entra MFA":
                    builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryInteractive;
                    if (!string.IsNullOrWhiteSpace(txtUsername.Text))
                        builder.UserID = txtUsername.Text;
                    break;
                case "Microsoft Entra Integrated":
                    builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryIntegrated;
                    break;
                case "Microsoft Entra Password":
                    builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryPassword;
                    builder.UserID = txtUsername.Text;
                    builder.Password = txtPassword.Text;
                    break;
            }

            return builder.ConnectionString;
        }
    }
}
