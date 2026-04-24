using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

internal static class ModernUi
{
    public static readonly Color Navy = Color.FromArgb(0, 48, 104);
    public static readonly Color Navy2 = Color.FromArgb(0, 60, 130);
    public static readonly Color Blue = Color.FromArgb(21, 96, 205);
    public static readonly Color LightBlue = Color.FromArgb(232, 241, 255);
    public static readonly Color Surface = Color.FromArgb(246, 248, 252);
    public static readonly Color Card = Color.White;
    public static readonly Color Border = Color.FromArgb(216, 225, 236);
    public static readonly Color Header = Color.FromArgb(245, 248, 252);
    public static readonly Color Text = Color.FromArgb(30, 41, 59);
    public static readonly Color Muted = Color.FromArgb(100, 116, 139);
    public static readonly Color Green = Color.FromArgb(37, 150, 60);
    public static readonly Color Orange = Color.FromArgb(245, 137, 18);
    public static readonly Color Red = Color.FromArgb(220, 48, 53);
    public static readonly Color Purple = Color.FromArgb(125, 54, 190);
    public static readonly Color Teal = Color.FromArgb(6, 137, 159);

    public static Font Font(float size, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);

    public static Label Label(string text, float size = 9.5f, FontStyle style = FontStyle.Regular, Color? color = null)
    {
        return new Label
        {
            Text = text,
            Font = Font(size, style),
            ForeColor = color ?? Text,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    public static RoundedPanel CardPanel(int radius = 7)
    {
        return new RoundedPanel
        {
            Radius = radius,
            BackColor = Card,
            BorderColor = Border,
            Padding = new Padding(12)
        };
    }

    public static RoundedPanel SearchBox(string placeholder = "Tìm kiếm nhanh...", int width = 310, int height = 36)
    {
        var wrapper = CardPanel(4);
        wrapper.Size = new Size(width, height);
        wrapper.BackColor = Color.White;
        wrapper.BorderColor = Color.FromArgb(207, 216, 228);
        wrapper.Padding = new Padding(0);

        var input = new TextBox
        {
            PlaceholderText = placeholder,
            BorderStyle = BorderStyle.None,
            Font = Font(9.5f),
            ForeColor = Text,
            BackColor = Color.White,
            Location = new Point(14, 9),
            Width = width - 54,
            Height = height - 12
        };
        wrapper.Controls.Add(input);

        var icon = Label("⌕", 18f, FontStyle.Regular, Color.FromArgb(103, 116, 136));
        icon.Location = new Point(width - 39, 3);
        icon.Size = new Size(30, height - 6);
        icon.TextAlign = ContentAlignment.MiddleCenter;
        wrapper.Controls.Add(icon);

        return wrapper;
    }

    public static Button IconButton(string text, int size = 34)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(size, size),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(72, 85, 104),
            FlatStyle = FlatStyle.Flat,
            Font = Font(15f, FontStyle.Regular),
            Cursor = Cursors.Hand,
            TabStop = false,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = LightBlue;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(221, 233, 251);
        return button;
    }

    public static Button Button(string text, Color backColor, int width = 128, int height = 38)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(width, height),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = Font(9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.12f);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.06f);
        return button;
    }

    public static Button OutlineButton(string text, int width = 120, int height = 34)
    {
        var button = Button(text, Color.White, width, height);
        button.ForeColor = Blue;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = LightBlue;
        return button;
    }

    public static TextBox TextBox(string placeholder = "", int width = 220)
    {
        return new TextBox
        {
            Width = width,
            Height = 30,
            Font = Font(9.5f),
            PlaceholderText = placeholder,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    public static ComboBox ComboBox(IEnumerable<string> items, int width = 170)
    {
        var combo = new ComboBox
        {
            Width = width,
            Height = 30,
            Font = Font(9.5f),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        combo.Items.AddRange(items.Cast<object>().ToArray());
        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
        return combo;
    }

    public static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            EnableHeadersVisualStyles = false,
            GridColor = Color.FromArgb(226, 232, 240),
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            RowTemplate = { Height = 34 },
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = Header;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = Font(8.8f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.DefaultCellStyle.Font = Font(8.8f);
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(217, 232, 252);
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);
        return grid;
    }

    public static void AddColumn(DataGridView grid, string header, string property, float fill = 1f, DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            FillWeight = fill,
            DefaultCellStyle = { Alignment = align }
        });
    }

    public static RoundedPanel Section(string title, int width, int height)
    {
        var panel = CardPanel();
        panel.Size = new Size(width, height);

        var label = Label(title.ToUpperInvariant(), 9.5f, FontStyle.Bold, Blue);
        label.Location = new Point(12, 8);
        label.Size = new Size(width - 24, 24);
        panel.Controls.Add(label);
        return panel;
    }

    public static Label Badge(string text, Color color)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Height = 22,
            Padding = new Padding(8, 2, 8, 2),
            Font = Font(8.4f, FontStyle.Bold),
            ForeColor = color,
            BackColor = Color.FromArgb(245, 250, 255),
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    public static RoundedPanel StatCard(string title, string value, string unit, Color accent, string iconText, string trend, int width, int height = 126)
    {
        var card = CardPanel();
        card.Size = new Size(width, height);

        var titleLabel = Label(title.ToUpperInvariant(), 8.4f, FontStyle.Bold, accent);
        titleLabel.Location = new Point(14, 12);
        titleLabel.Size = new Size(width - 28, 22);
        titleLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(titleLabel);

        var icon = new CircleLabel
        {
            Text = iconText,
            CircleColor = accent,
            ForeColor = Color.White,
            Font = Font(24f, FontStyle.Bold),
            Location = new Point(24, 42),
            Size = new Size(58, 58),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(icon);

        float valueSize = value.Length > 11 ? 13.5f : value.Length > 7 ? 18f : 20f;
        var valueLabel = Label(value, valueSize, FontStyle.Bold, accent);
        valueLabel.Location = new Point(92, 42);
        valueLabel.Size = new Size(width - 104, 34);
        valueLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(valueLabel);

        var unitLabel = Label(unit, 9f, FontStyle.Regular, Muted);
        unitLabel.Location = new Point(92, 76);
        unitLabel.Size = new Size(width - 104, 20);
        unitLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(unitLabel);

        var trendLabel = Label(trend, 8.7f, FontStyle.Regular, Green);
        trendLabel.Location = new Point(18, height - 28);
        trendLabel.Size = new Size(width - 36, 22);
        trendLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(trendLabel);

        return card;
    }

    public static void ApplyFormDefaults(Form form, Size minimum)
    {
        form.Font = Font(9.5f);
        form.BackColor = Surface;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.MinimumSize = minimum;
        form.AutoScaleMode = AutoScaleMode.Dpi;
    }

    public static void AddRows(DataGridView grid, params object[] rows)
    {
        grid.AutoGenerateColumns = false;
        grid.DataSource = rows.ToList();
    }
}

internal sealed class RoundedPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 7;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = ModernUi.Border;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BorderThickness { get; set; } = 1;

    public RoundedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new SolidBrush(Parent?.BackColor ?? ModernUi.Surface);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        using var path = RoundedRect(rect, Radius);
        using var brush = new SolidBrush(BackColor);
        using var pen = new Pen(BorderColor, BorderThickness);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            path.CloseFigure();
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class CircleLabel : Label
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color CircleColor { get; set; } = ModernUi.Blue;

    public CircleLabel()
    {
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(CircleColor);
        e.Graphics.FillEllipse(brush, 0, 0, Width - 1, Height - 1);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

internal sealed class BarChartPanel : Control
{
    public List<(string Label, int Value)> Bars { get; } = new();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BarColor { get; set; } = ModernUi.Blue;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int AxisMax { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SeriesLabel { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowValueLabels { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int GridSteps { get; set; } = 4;

    public BarChartPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var plot = new Rectangle(56, 18, Width - 76, Height - 62);
        using var axisPen = new Pen(Color.FromArgb(199, 210, 224));
        using var gridPen = new Pen(Color.FromArgb(233, 238, 246));
        using var textBrush = new SolidBrush(ModernUi.Muted);
        using var barBrush = new SolidBrush(BarColor);

        long max = Math.Max(1, AxisMax > 0 ? AxisMax : Bars.Count == 0 ? 1 : Bars.Max(b => b.Value));
        int steps = Math.Max(1, GridSteps);
        for (int i = 0; i <= steps; i++)
        {
            int y = plot.Bottom - i * plot.Height / steps;
            e.Graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            string label = (max * i / steps).ToString("N0");
            e.Graphics.DrawString(label, ModernUi.Font(7.6f), textBrush, 0, y - 8);
        }

        e.Graphics.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
        if (Bars.Count == 0)
        {
            return;
        }

        int slot = Math.Max(1, plot.Width / Bars.Count);
        int barWidth = Math.Max(16, Math.Min(34, slot - 20));
        for (int i = 0; i < Bars.Count; i++)
        {
            int barHeight = Math.Max(4, (int)(Bars[i].Value * (long)plot.Height / max));
            int x = plot.Left + i * slot + (slot - barWidth) / 2;
            int y = plot.Bottom - barHeight;
            e.Graphics.FillRectangle(barBrush, x, y, barWidth, barHeight);
            if (ShowValueLabels)
            {
                e.Graphics.DrawString(Bars[i].Value.ToString("N0"), ModernUi.Font(8f, FontStyle.Bold), Brushes.Black, x - 3, y - 18);
            }
            e.Graphics.DrawString(Bars[i].Label, ModernUi.Font(8f), textBrush, x - 4, plot.Bottom + 8);
        }

        if (!string.IsNullOrWhiteSpace(SeriesLabel))
        {
            int legendY = Height - 24;
            int legendX = Math.Max(0, Width / 2 - 80);
            e.Graphics.FillRectangle(barBrush, legendX, legendY + 3, 14, 14);
            e.Graphics.DrawString(SeriesLabel, ModernUi.Font(8.5f), textBrush, legendX + 22, legendY);
        }
    }
}

internal sealed class DonutChartPanel : Control
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Percent { get; set; } = 81;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor { get; set; } = ModernUi.Green;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CenterText { get; set; } = "81%";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SubText { get; set; } = "Đang ở";

    public DonutChartPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(18, 14, Math.Min(Height - 28, Width / 2 - 20), Math.Min(Height - 28, Width / 2 - 20));
        using var bgPen = new Pen(Color.FromArgb(214, 219, 228), 28) { StartCap = LineCap.Flat, EndCap = LineCap.Flat };
        using var accentPen = new Pen(AccentColor, 28) { StartCap = LineCap.Flat, EndCap = LineCap.Flat };
        e.Graphics.DrawArc(bgPen, rect, -90, 360);
        e.Graphics.DrawArc(accentPen, rect, -90, Math.Max(0, Math.Min(100, Percent)) * 360 / 100f);

        using var textBrush = new SolidBrush(ModernUi.Text);
        using var mutedBrush = new SolidBrush(ModernUi.Muted);
        var center = rect;
        e.Graphics.DrawString(CenterText, ModernUi.Font(18f, FontStyle.Bold), new SolidBrush(AccentColor), center, CenterFormat());
        var subRect = new Rectangle(center.Left, center.Top + 28, center.Width, 24);
        e.Graphics.DrawString(SubText, ModernUi.Font(9f, FontStyle.Bold), textBrush, subRect, CenterFormat());

        int legendX = rect.Right + 42;
        DrawLegend(e.Graphics, legendX, rect.Top + 36, AccentColor, "Đang ở", "692 (81%)");
        DrawLegend(e.Graphics, legendX, rect.Top + 76, Color.FromArgb(214, 219, 228), "Còn trống", "164 (19%)");
    }

    private static StringFormat CenterFormat() => new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };

    private static void DrawLegend(Graphics graphics, int x, int y, Color color, string label, string value)
    {
        using var brush = new SolidBrush(color);
        using var textBrush = new SolidBrush(ModernUi.Text);
        graphics.FillRectangle(brush, x, y + 2, 16, 16);
        graphics.DrawString(label, ModernUi.Font(9f), textBrush, x + 26, y - 2);
        graphics.DrawString(value, ModernUi.Font(9f), textBrush, x + 118, y - 2);
    }
}

internal sealed class WarningTriangleControl : Control
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TriangleColor { get; set; } = ModernUi.Orange;

    public WarningTriangleControl()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var points = new[]
        {
            new Point(Width / 2, 4),
            new Point(Width - 5, Height - 5),
            new Point(5, Height - 5)
        };
        using var brush = new SolidBrush(TriangleColor);
        e.Graphics.FillPolygon(brush, points);

        TextRenderer.DrawText(
            e.Graphics,
            "!",
            ModernUi.Font(28f, FontStyle.Bold),
            new Rectangle(0, Height / 3 - 4, Width, Height / 2),
            Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

internal sealed class QrPanel : Control
{
    public QrPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        using var black = new SolidBrush(Color.Black);
        using var blue = new SolidBrush(ModernUi.Blue);
        int size = Math.Min(Width, Height) - 12;
        int cell = size / 19;
        int ox = (Width - cell * 19) / 2;
        int oy = (Height - cell * 19) / 2;

        bool[,] matrix = BuildMatrix();
        for (int y = 0; y < 19; y++)
        {
            for (int x = 0; x < 19; x++)
            {
                if (matrix[x, y])
                {
                    e.Graphics.FillRectangle(black, ox + x * cell, oy + y * cell, cell, cell);
                }
            }
        }

        e.Graphics.DrawString("VIETQR", ModernUi.Font(7.5f, FontStyle.Bold), blue, ox + 4, oy - 14);
    }

    private static bool[,] BuildMatrix()
    {
        var matrix = new bool[19, 19];
        int[,] eyes = { { 0, 0 }, { 12, 0 }, { 0, 12 } };
        for (int i = 0; i < eyes.GetLength(0); i++)
        {
            int ex = eyes[i, 0];
            int ey = eyes[i, 1];
            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < 7; x++)
                {
                    bool border = x == 0 || y == 0 || x == 6 || y == 6;
                    bool center = x >= 2 && x <= 4 && y >= 2 && y <= 4;
                    matrix[ex + x, ey + y] = border || center;
                }
            }
        }

        for (int y = 0; y < 19; y++)
        {
            for (int x = 0; x < 19; x++)
            {
                if (matrix[x, y])
                {
                    continue;
                }

                matrix[x, y] = ((x * 13 + y * 7 + x * y) % 5 == 0) || ((x + y) % 7 == 0);
            }
        }

        return matrix;
    }
}
