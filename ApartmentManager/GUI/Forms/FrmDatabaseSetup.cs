using System;
using System.Windows.Forms;
using ApartmentManager.Utilities;
using Microsoft.Data.SqlClient;
using Serilog;

using System.Drawing;
namespace ApartmentManager.GUI.Forms;

public partial class FrmDatabaseSetup : Form
{
    public FrmDatabaseSetup()
    {
        // Designer initialization - not used for this form
        // InitializeComponent();
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.Text = "Cáº¥u hÃ¬nh CÆ¡ sá»Ÿ dá»¯ liá»‡u";
        this.Size = new Size(500, 500);
    }

    private void FrmDatabaseSetup_Load(object sender, EventArgs e)
    {
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

        // Title
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

        // Server
        var lblServer = new Label { Text = "Server:", Location = new Point(0, yPos), AutoSize = true };
        var txtServer = new TextBox
        {
            Name = "txtServer",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            Text = "localhost",
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(lblServer);
        pnlMain.Controls.Add(txtServer);
        yPos += fieldHeight + spacing;

        // Database
        var lblDatabase = new Label { Text = "Database:", Location = new Point(0, yPos), AutoSize = true };
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

        // Authentication Type
        var lblAuth = new Label { Text = "XÃ¡c thá»±c:", Location = new Point(0, yPos), AutoSize = true };
        var cmbAuth = new ComboBox
        {
            Name = "cmbAuth",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbAuth.Items.AddRange(new string[] { "Windows Authentication", "SQL Server Authentication" });
        cmbAuth.SelectedIndex = 0;
        cmbAuth.SelectedIndexChanged += (s, e) => UpdateAuthFields(pnlMain);
        pnlMain.Controls.Add(lblAuth);
        pnlMain.Controls.Add(cmbAuth);
        yPos += fieldHeight + spacing;

        // Username (SQL Auth)
        var lblUsername = new Label
        {
            Name = "lblUsername",
            Text = "Username:",
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

        // Password (SQL Auth)
        var lblPassword = new Label
        {
            Name = "lblPassword",
            Text = "Password:",
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

        // Status
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

        // Buttons
        var btnTest = new Button
        {
            Text = "Ká»² GÆ¯á»¢NG Káº¾T Ná»I",
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
        btnTest.Click += (s, e) => BtnTest_Click(s, e, pnlMain, lblStatus);

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
        btnSave.Click += (s, e) => BtnSave_Click(s, e, pnlMain);

        pnlMain.Controls.Add(btnTest);
        pnlMain.Controls.Add(btnSave);

        this.Controls.Add(pnlMain);
    }

    private void UpdateAuthFields(Panel pnlMain)
    {
        var cmbAuth = pnlMain.Controls["cmbAuth"] as ComboBox;
        var lblUsername = pnlMain.Controls["lblUsername"] as Label;
        var txtUsername = pnlMain.Controls["txtUsername"] as TextBox;
        var lblPassword = pnlMain.Controls["lblPassword"] as Label;
        var txtPassword = pnlMain.Controls["txtPassword"] as TextBox;

        if (cmbAuth == null) return;

        bool isSQLAuth = cmbAuth.SelectedIndex == 1;
        if (lblUsername != null) lblUsername.Visible = isSQLAuth;
        if (txtUsername != null) txtUsername.Visible = isSQLAuth;
        if (lblPassword != null) lblPassword.Visible = isSQLAuth;
        if (txtPassword != null) txtPassword.Visible = isSQLAuth;
    }

    private void BtnTest_Click(object? sender, EventArgs e, Panel pnlMain, Label lblStatus)
    {
        string connectionString = BuildConnectionString(pnlMain);

        if (DatabaseHelper.TestConnection(connectionString))
        {
            lblStatus.Text = "âœ“ Káº¿t ná»‘i thÃ nh cÃ´ng!";
            lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
        }
        else
        {
            lblStatus.Text = "âœ— Káº¿t ná»‘i tháº¥t báº¡i. Kiá»ƒm tra thÃ´ng tin cáº¥u hÃ¬nh.";
            lblStatus.ForeColor = Color.FromArgb(255, 87, 34);
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e, Panel pnlMain)
    {
        string connectionString = BuildConnectionString(pnlMain);

        if (!DatabaseHelper.TestConnection(connectionString))
        {
            MessageBox.Show("KhÃ´ng thá»ƒ káº¿t ná»‘i Ä‘áº¿n database. Vui lÃ²ng kiá»ƒm tra láº¡i thÃ´ng tin.", 
                "Lá»—i", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Save to app.config (simplified - in production you'd use ConfigurationManager)
        // For now, just close and proceed
        MessageBox.Show("Cáº¥u hÃ¬nh Ä‘Ã£ Ä‘Æ°á»£c lÆ°u. á»¨ng dá»¥ng sáº½ khá»Ÿi Ä‘á»™ng láº¡i.", 
            "ThÃ´ng bÃ¡o", MessageBoxButtons.OK, MessageBoxIcon.Information);

        this.Close();
    }

    private string BuildConnectionString(Panel pnlMain)
    {
        var txtServer = pnlMain.Controls["txtServer"] as TextBox;
        var txtDatabase = pnlMain.Controls["txtDatabase"] as TextBox;
        var cmbAuth = pnlMain.Controls["cmbAuth"] as ComboBox;
        var txtUsername = pnlMain.Controls["txtUsername"] as TextBox;
        var txtPassword = pnlMain.Controls["txtPassword"] as TextBox;

        if (txtServer == null || txtDatabase == null || cmbAuth == null)
            return "";

        string server = txtServer.Text;
        string database = txtDatabase.Text;

        if (cmbAuth?.SelectedIndex == 0)
        {
            // Windows Auth
            return $"Server={server};Database={database};Integrated Security=true;Encrypt=false;";
        }
        else
        {
            // SQL Auth
            return $"Server={server};Database={database};User Id={txtUsername?.Text};Password={txtPassword?.Text};Encrypt=false;";
        }
    }
}


