using ApartmentManager.Utilities;
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmDatabaseSetup : Form
{
    public FrmDatabaseSetup()
    {
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Text = "Cáº¥u hÃ¬nh cÆ¡ sá»Ÿ dá»¯ liá»‡u";
        Size = new Size(500, 500);

        ConfigureUI();
    }

    private void ConfigureUI()
    {
        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 245, 250),
            Padding = new Padding(30)
        };

        int yPos = 10;
        const int fieldHeight = 25;
        const int spacing = 10;
        const int labelWidth = 120;
        const int controlWidth = 300;

        var lblTitle = new Label
        {
            Text = "Cáº¤U HÃŒNH Káº¾T Ná»I CSDL",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 86, 155),
            Width = controlWidth,
            Location = new Point(30, yPos)
        };
        pnlMain.Controls.Add(lblTitle);
        yPos += 50;

        var lblServer = new Label { Text = "MÃ¡y chá»§:", Location = new Point(0, yPos), AutoSize = true };
        var txtServer = new TextBox
        {
            Name = "txtServer",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            Text = @".\SQLEXPRESS",
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(lblServer);
        pnlMain.Controls.Add(txtServer);
        yPos += fieldHeight + spacing;

        var lblDatabase = new Label { Text = "CÆ¡ sá»Ÿ dá»¯ liá»‡u:", Location = new Point(0, yPos), AutoSize = true };
        var txtDatabase = new TextBox
        {
            Name = "txtDatabase",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            Text = "ApartmentManagerDB",
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(lblDatabase);
        pnlMain.Controls.Add(txtDatabase);
        yPos += fieldHeight + spacing;

        var lblAuth = new Label { Text = "XÃ¡c thá»±c:", Location = new Point(0, yPos), AutoSize = true };
        var cmbAuth = new ComboBox
        {
            Name = "cmbAuth",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbAuth.Items.AddRange(new object[] { "XÃ¡c thá»±c Windows", "XÃ¡c thá»±c SQL Server" });
        cmbAuth.SelectedIndex = 0;
        cmbAuth.SelectedIndexChanged += (s, e) => UpdateAuthFields(pnlMain);
        pnlMain.Controls.Add(lblAuth);
        pnlMain.Controls.Add(cmbAuth);
        yPos += fieldHeight + spacing;

        var lblUsername = new Label
        {
            Name = "lblUsername",
            Text = "TÃªn ngÆ°á»i dÃ¹ng:",
            Location = new Point(0, yPos),
            AutoSize = true,
            Visible = false
        };
        var txtUsername = new TextBox
        {
            Name = "txtUsername",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };
        pnlMain.Controls.Add(lblUsername);
        pnlMain.Controls.Add(txtUsername);
        yPos += fieldHeight + spacing;

        var lblPassword = new Label
        {
            Name = "lblPassword",
            Text = "Máº­t kháº©u:",
            Location = new Point(0, yPos),
            AutoSize = true,
            Visible = false
        };
        var txtPassword = new TextBox
        {
            Name = "txtPassword",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            UseSystemPasswordChar = true,
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };
        pnlMain.Controls.Add(lblPassword);
        pnlMain.Controls.Add(txtPassword);
        yPos += fieldHeight + spacing + 20;

        var lblStatus = new Label
        {
            Name = "lblStatus",
            Text = "",
            Width = controlWidth + 100,
            Height = 40,
            Location = new Point(0, yPos),
            AutoSize = false,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(255, 87, 34)
        };
        pnlMain.Controls.Add(lblStatus);
        yPos += 50;

        var btnTest = new Button
        {
            Text = "KIá»‚M TRA Káº¾T Ná»I",
            Width = 150,
            Height = 40,
            Location = new Point(0, yPos),
            BackColor = Color.FromArgb(33, 150, 243),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnTest.FlatAppearance.BorderSize = 0;
        btnTest.Click += (s, e) => BtnTest_Click(pnlMain, lblStatus);

        var btnSave = new Button
        {
            Text = "LÆ¯U VÃ€ TIáº¾P Tá»¤C",
            Width = 150,
            Height = 40,
            Location = new Point(160, yPos),
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (s, e) => BtnSave_Click(pnlMain);

        pnlMain.Controls.Add(btnTest);
        pnlMain.Controls.Add(btnSave);

        Controls.Add(pnlMain);
        LoadCurrentSettings(pnlMain);
    }

    private void UpdateAuthFields(Panel pnlMain)
    {
        var cmbAuth = pnlMain.Controls["cmbAuth"] as ComboBox;
        var lblUsername = pnlMain.Controls["lblUsername"] as Label;
        var txtUsername = pnlMain.Controls["txtUsername"] as TextBox;
        var lblPassword = pnlMain.Controls["lblPassword"] as Label;
        var txtPassword = pnlMain.Controls["txtPassword"] as TextBox;

        if (cmbAuth == null)
        {
            return;
        }

        bool isSqlAuth = cmbAuth.SelectedIndex == 1;
        if (lblUsername != null) lblUsername.Visible = isSqlAuth;
        if (txtUsername != null) txtUsername.Visible = isSqlAuth;
        if (lblPassword != null) lblPassword.Visible = isSqlAuth;
        if (txtPassword != null) txtPassword.Visible = isSqlAuth;
    }

    private void BtnTest_Click(Panel pnlMain, Label lblStatus)
    {
        var connectionString = BuildConnectionString(pnlMain);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            lblStatus.Text = "Vui lòng nhập đầy đủ thông tin kết nối.";
            lblStatus.ForeColor = Color.FromArgb(255, 87, 34);
            return;
        }

        var (success, message) = DatabaseHelper.TestConnection(connectionString);
        lblStatus.Text = success ? "âœ“ Káº¿t ná»‘i thÃ nh cÃ´ng!" : $"âœ— {message}";
        lblStatus.ForeColor = success
            ? Color.FromArgb(76, 175, 80)
            : Color.FromArgb(255, 87, 34);
    }

    private void BtnSave_Click(Panel pnlMain)
    {
        var connectionString = BuildConnectionString(pnlMain);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ thông tin kết nối.",
                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var (success, message) = DatabaseHelper.TestConnection(connectionString);
        if (!success)
        {
            MessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!DatabaseHelper.SaveConnectionString(connectionString, out var saveMessage))
        {
            MessageBox.Show(saveMessage, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("Cáº¥u hÃ¬nh Ä‘Ã£ Ä‘Æ°á»£c lÆ°u. á»¨ng dá»¥ng sáº½ khá»Ÿi Ä‘á»™ng láº¡i.",
            "ThÃ´ng bÃ¡o", MessageBoxButtons.OK, MessageBoxIcon.Information);

        DialogResult = DialogResult.OK;
        Close();
    }

    private string BuildConnectionString(Panel pnlMain)
    {
        var txtServer = pnlMain.Controls["txtServer"] as TextBox;
        var txtDatabase = pnlMain.Controls["txtDatabase"] as TextBox;
        var cmbAuth = pnlMain.Controls["cmbAuth"] as ComboBox;
        var txtUsername = pnlMain.Controls["txtUsername"] as TextBox;
        var txtPassword = pnlMain.Controls["txtPassword"] as TextBox;

        if (txtServer == null || txtDatabase == null || cmbAuth == null)
        {
            return string.Empty;
        }

        string server = txtServer.Text.Trim();
        string database = txtDatabase.Text.Trim();
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
        {
            return string.Empty;
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            Encrypt = false,
            TrustServerCertificate = true
        };

        if (cmbAuth.SelectedIndex == 0)
        {
            builder.IntegratedSecurity = true;
            return builder.ConnectionString;
        }

        if (string.IsNullOrWhiteSpace(txtUsername?.Text) || string.IsNullOrWhiteSpace(txtPassword?.Text))
        {
            return string.Empty;
        }

        builder.UserID = txtUsername.Text.Trim();
        builder.Password = txtPassword.Text;
        builder.IntegratedSecurity = false;
        return builder.ConnectionString;
    }

    private void LoadCurrentSettings(Panel pnlMain)
    {
        var txtServer = pnlMain.Controls["txtServer"] as TextBox;
        var txtDatabase = pnlMain.Controls["txtDatabase"] as TextBox;
        var cmbAuth = pnlMain.Controls["cmbAuth"] as ComboBox;
        var txtUsername = pnlMain.Controls["txtUsername"] as TextBox;
        var txtPassword = pnlMain.Controls["txtPassword"] as TextBox;

        if (txtServer == null || txtDatabase == null || cmbAuth == null)
        {
            return;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(DatabaseHelper.GetConnectionString());
            txtServer.Text = string.IsNullOrWhiteSpace(builder.DataSource) ? @".\SQLEXPRESS" : builder.DataSource;
            txtDatabase.Text = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "ApartmentManagerDB" : builder.InitialCatalog;
            cmbAuth.SelectedIndex = builder.IntegratedSecurity ? 0 : 1;

            if (txtUsername != null)
            {
                txtUsername.Text = builder.UserID;
            }

            if (txtPassword != null)
            {
                txtPassword.Text = builder.Password;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load current database settings");
        }
    }
}
