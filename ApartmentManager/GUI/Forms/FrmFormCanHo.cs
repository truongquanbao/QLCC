using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms
{
    public class FrmFormCanHo : Form
    {
        public CanHo CanHo { get; private set; }
        private TextBox txtMa = null!;
        private TextBox txtTang = null!;
        private TextBox txtDienTich = null!;
        private ComboBox cboLoai = null!;
        private ComboBox cboTrangThai = null!;

        public FrmFormCanHo(CanHo canHo, List<CanHo> dsCanHo)
        {
            CanHo = canHo != null
                ? new CanHo
                {
                    MaCanHo = canHo.MaCanHo,
                    Tang = canHo.Tang,
                    LoaiCanHo = canHo.LoaiCanHo,
                    DienTich = canHo.DienTich,
                    TrangThai = canHo.TrangThai,
                    MaCuDan = canHo.MaCuDan
                }
                : new CanHo();

            bool isEdit = canHo != null;

            Text = isEdit ? "Sửa căn hộ" : "Thêm căn hộ";
            Size = new Size(380, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9.5f);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(16),
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            void AddRow(string lbl, Control ctrl)
            {
                layout.Controls.Add(new Label { Text = lbl, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
                ctrl.Dock = DockStyle.Fill;
                layout.Controls.Add(ctrl);
            }

            txtMa = new TextBox { Text = CanHo.MaCanHo, ReadOnly = isEdit };
            txtTang = new TextBox { Text = CanHo.Tang };
            cboLoai = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboLoai.Items.AddRange(new[]
            {
                "Studio",
                "1 phòng ngủ",
                "2 phòng ngủ",
                "3 phòng ngủ",
                "Penthouse",
                "Khác"
            });
            cboLoai.Text = CanHo.LoaiCanHo;
            txtDienTich = new TextBox { Text = CanHo.DienTich.ToString() };
            cboTrangThai = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboTrangThai.Items.AddRange(new[] { "Đang ở", "Trống", "Đang sửa" });
            cboTrangThai.Text = CanHo.TrangThai;

            AddRow("Mã căn hộ:", txtMa);
            AddRow("Tầng:", txtTang);
            AddRow("Loại:", cboLoai);
            AddRow("Diện tích (m²):", txtDienTich);
            AddRow("Trạng thái:", cboTrangThai);

            var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(12, 8, 12, 8) };
            var btnOK = new Button
            {
                Text = "Lưu",
                Width = 90,
                Height = 28,
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White
            };
            var btnCancel = new Button { Text = "Hủy", Width = 80, Height = 28, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            btnOK.Left = pnlBtn.Width - 185;
            btnOK.Top = 8;
            btnCancel.Left = pnlBtn.Width - 90;
            btnCancel.Top = 8;
            pnlBtn.Controls.AddRange(new Control[] { btnOK, btnCancel });
            pnlBtn.Resize += (s, e) =>
            {
                btnOK.Left = pnlBtn.Width - 185;
                btnCancel.Left = pnlBtn.Width - 90;
            };

            btnOK.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtMa.Text))
                {
                    MessageBox.Show("Vui lòng nhập mã căn hộ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                CanHo.MaCanHo = txtMa.Text.Trim();
                CanHo.Tang = txtTang.Text.Trim();
                CanHo.LoaiCanHo = cboLoai.Text;
                double.TryParse(txtDienTich.Text, out double dt);
                CanHo.DienTich = dt;
                CanHo.TrangThai = cboTrangThai.Text;
                DialogResult = DialogResult.OK;
            };

            Controls.AddRange(new Control[] { pnlBtn, layout });
        }
    }

    public class FrmFormCuDan : Form
    {
        public CuDan CuDan { get; private set; }
        private TextBox txtMa = null!;
        private TextBox txtHoTen = null!;
        private TextBox txtCCCD = null!;
        private TextBox txtSDT = null!;
        private ComboBox cboCanHo = null!;
        private DateTimePicker dtpNgayVao = null!;

        public FrmFormCuDan(CuDan cuDan, List<CanHo> dsCanHo)
        {
            CuDan = cuDan != null
                ? new CuDan
                {
                    MaCuDan = cuDan.MaCuDan,
                    HoTen = cuDan.HoTen,
                    CCCD = cuDan.CCCD,
                    SoDienThoai = cuDan.SoDienThoai,
                    MaCanHo = cuDan.MaCanHo,
                    NgayVao = cuDan.NgayVao
                }
                : new CuDan { NgayVao = DateTime.Today };

            bool isEdit = cuDan != null;

            Text = isEdit ? "Sửa cư dân" : "Thêm cư dân";
            Size = new Size(380, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9.5f);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(16),
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            void AddRow(string lbl, Control ctrl)
            {
                layout.Controls.Add(new Label { Text = lbl, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
                ctrl.Dock = DockStyle.Fill;
                layout.Controls.Add(ctrl);
            }

            txtMa = new TextBox { Text = CuDan.MaCuDan, ReadOnly = isEdit };
            txtHoTen = new TextBox { Text = CuDan.HoTen };
            txtCCCD = new TextBox { Text = CuDan.CCCD };
            txtSDT = new TextBox { Text = CuDan.SoDienThoai };
            cboCanHo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboCanHo.Items.Add(string.Empty);
            cboCanHo.Items.AddRange(dsCanHo.Select(c => c.MaCanHo).ToArray());
            cboCanHo.Text = CuDan.MaCanHo;
            dtpNgayVao = new DateTimePicker { Value = CuDan.NgayVao, Format = DateTimePickerFormat.Short };

            AddRow("Mã cư dân:", txtMa);
            AddRow("Họ tên:", txtHoTen);
            AddRow("CCCD:", txtCCCD);
            AddRow("Số điện thoại:", txtSDT);
            AddRow("Căn hộ:", cboCanHo);
            AddRow("Ngày vào:", dtpNgayVao);

            var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(12, 8, 12, 8) };
            var btnOK = new Button
            {
                Text = "Lưu",
                Width = 90,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White
            };
            var btnCancel = new Button { Text = "Hủy", Width = 80, Height = 28, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            btnCancel.Left = 180;
            btnCancel.Top = 8;
            btnOK.Left = 85;
            btnOK.Top = 8;

            btnOK.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtHoTen.Text))
                {
                    MessageBox.Show("Vui lòng nhập họ tên!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                CuDan.MaCuDan = txtMa.Text.Trim();
                CuDan.HoTen = txtHoTen.Text.Trim();
                CuDan.CCCD = txtCCCD.Text.Trim();
                CuDan.SoDienThoai = txtSDT.Text.Trim();
                CuDan.MaCanHo = cboCanHo.Text;
                CuDan.NgayVao = dtpNgayVao.Value;
                DialogResult = DialogResult.OK;
            };

            pnlBtn.Controls.AddRange(new Control[] { btnOK, btnCancel });
            Controls.AddRange(new Control[] { pnlBtn, layout });
        }
    }
}
