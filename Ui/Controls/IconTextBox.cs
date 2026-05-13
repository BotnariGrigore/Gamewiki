using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GameWikiApp.UI.Controls
{
    public class IconTextBox : UserControl
    {
        private TextBox _textBox;

        public IconTextBox()
        {
            Height = 36;
            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(4, 4),
                Width = Width - 8,
                Height = Height - 8,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_textBox);
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
    }
}