using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

internal sealed class AccountAvatarControl : Control
{
    private Image? _avatarImage;

    public AccountAvatarControl()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = Color.Transparent;
    }

    public string Initials { get; private set; } = "U";

    public void SetAvatar(string? avatarPath, string? fallbackInitials = null)
    {
        Initials = string.IsNullOrWhiteSpace(fallbackInitials) ? "U" : fallbackInitials.Trim().ToUpperInvariant();

        if (_avatarImage != null)
        {
            _avatarImage.Dispose();
            _avatarImage = null;
        }

        if (!string.IsNullOrWhiteSpace(avatarPath) && File.Exists(avatarPath))
        {
            try
            {
                using var stream = new FileStream(avatarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var source = Image.FromStream(stream);
                _avatarImage = new Bitmap(source);
            }
            catch
            {
                _avatarImage = null;
            }
        }

        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _avatarImage?.Dispose();
            _avatarImage = null;
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var bounds = new Rectangle(1, 1, Width - 3, Height - 3);
        using var bg = new SolidBrush(Color.FromArgb(228, 238, 251));
        using var border = new Pen(Color.FromArgb(198, 214, 236));
        e.Graphics.FillEllipse(bg, bounds);

        if (_avatarImage != null)
        {
            using var clipPath = new System.Drawing.Drawing2D.GraphicsPath();
            clipPath.AddEllipse(bounds);
            var state = e.Graphics.Save();
            e.Graphics.SetClip(clipPath);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(_avatarImage, bounds);
            e.Graphics.Restore(state);
            e.Graphics.DrawEllipse(border, bounds);
            return;
        }

        e.Graphics.DrawEllipse(border, bounds);

        using var hair = new SolidBrush(Color.FromArgb(37, 48, 66));
        using var skin = new SolidBrush(Color.FromArgb(255, 190, 139));
        using var shirt = new SolidBrush(ModernUi.Blue);
        using var neck = new SolidBrush(Color.FromArgb(238, 164, 116));

        int cx = Width / 2;
        e.Graphics.FillEllipse(hair, cx - 28, 26, 56, 44);
        e.Graphics.FillEllipse(skin, cx - 24, 34, 48, 52);
        e.Graphics.FillRectangle(neck, cx - 11, 78, 22, 18);
        e.Graphics.FillPie(shirt, cx - 46, 84, 92, 78, 200, 140);
        e.Graphics.FillRectangle(shirt, cx - 35, 95, 70, 34);

        using var eye = new SolidBrush(Color.FromArgb(35, 48, 65));
        e.Graphics.FillEllipse(eye, cx - 14, 56, 4, 4);
        e.Graphics.FillEllipse(eye, cx + 10, 56, 4, 4);
        using var mouth = new Pen(Color.FromArgb(140, 70, 68), 2);
        e.Graphics.DrawArc(mouth, cx - 10, 66, 20, 12, 15, 150);
    }
}
