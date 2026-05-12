using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GameWikiApp.UI.Controls
{
    public class IconTextBox : UserControl
    {
        private TextBox _textBox;
        private IconType _icon = IconType.User;

        public IconTextBox()
        {
            Height = 36;
            BackColor = Color.WhiteSmoke;
            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Location = new Point(44, 8),
                Width = 220,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.WhiteSmoke
            };
            Controls.Add(_textBox);
            Padding = new Padding(1);
            Resize += (s, e) => _textBox.Width = Width - 56;
        }

        public enum IconType { User, Lock }

        public void SetIcon(IconType type)
        {
            _icon = type;
            Invalidate();
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool UseSystemPasswordChar
        {
            get => _textBox.UseSystemPasswordChar;
            set => _textBox.UseSystemPasswordChar = value;
        }

        public string Value => _textBox.Text;

        public TextBox InnerTextBox => _textBox;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // draw left icon background circle
            var r = new Rectangle(4, 4, 28, 28);
            using var bg = new SolidBrush(Color.FromArgb(230, 230, 230));
            e.Graphics.FillEllipse(bg, r);

            using var pen = new Pen(Color.DimGray, 2);
            if (_icon == IconType.User)
            {
                e.Graphics.DrawEllipse(pen, 12, 7, 12, 9); // head
                e.Graphics.DrawArc(pen, 8, 14, 20, 14, 20, 140); // shoulders
            }
            else
            {
                e.Graphics.DrawRectangle(pen, 10, 14, 16, 10);
                e.Graphics.DrawArc(pen, 10, 6, 16, 16, 180, 180);
            }
        }
    }
}
