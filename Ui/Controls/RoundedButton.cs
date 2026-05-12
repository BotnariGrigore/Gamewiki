using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GameWikiApp.UI.Controls
{
    public class RoundedButton : Button
    {
        private int _radius = 12;

        [DefaultValue(12)]
        public int Radius
        {
            get => _radius;
            set { _radius = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = ClientRectangle;
            rect.Inflate(-1, -1);
            using var path = RoundedRect(rect, Radius);
            using var brush = new SolidBrush(BackColor);
            pevent.Graphics.FillPath(brush, path);
            using var pen = new Pen(FlatAppearance.BorderColor.IsEmpty ? ForeColor : FlatAppearance.BorderColor, 1);
            pevent.Graphics.DrawPath(pen, path);

            TextRenderer.DrawText(pevent.Graphics, Text, Font, rect, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
