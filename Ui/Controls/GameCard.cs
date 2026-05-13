using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using GameWikiApp.Helpers;

namespace GameWikiApp.Ui.Controls
{
    public class GameCard : UserControl
    {
        private PictureBox _pictureBox;
        private Label _lblName;
        private Label _lblGenre;
        private Label _lblRating;
        private Panel _ratingPanel;

        private string _gameName = "Unknown";
        private string _genre = "Action";
        private string _rating = "4.5";
        private Image? _gameImage;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string GameName
        {
            get => _gameName;
            set { _gameName = value; _lblName.Text = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Genre
        {
            get => _genre;
            set { _genre = value; _lblGenre.Text = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Rating
        {
            get => _rating;
            set { _rating = value; _lblRating.Text = "★ " + value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image? GameImage
        {
            get => _gameImage;
            set { _gameImage = value; _pictureBox.Image = value; }
        }

        public GameCard()
        {
            Size = new Size(220, 260);
            Margin = new Padding(15);

            // PictureBox (top)
            _pictureBox = new PictureBox
            {
                Size = new Size(220, 140),
                Location = new Point(0, 0),
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            Controls.Add(_pictureBox);

            // Game Name
            _lblName = new Label
            {
                Text = _gameName,
                Location = new Point(12, 150),
                AutoSize = true,
                MaximumSize = new Size(196, 30)
            };
            Controls.Add(_lblName);

            // Genre
            _lblGenre = new Label
            {
                Text = _genre,
                Location = new Point(12, 180),
                AutoSize = true
            };
            Controls.Add(_lblGenre);

            // Rating panel
            _ratingPanel = new Panel
            {
                Location = new Point(12, 205),
                Size = new Size(196, 28)
            };

            _lblRating = new Label
            {
                Text = "★ " + _rating,
                Location = new Point(8, 4),
                AutoSize = true
            };
            _ratingPanel.Controls.Add(_lblRating);
            Controls.Add(_ratingPanel);
        }

        public void SetPlaceholderImage(Color bgColor, string icon)
        {
            var bmp = new Bitmap(220, 140);
            using var g = Graphics.FromImage(bmp);
            g.Clear(bgColor);

            using var font = new Font("Segoe UI", 36);
            var sz = g.MeasureString(icon, font);
            g.DrawString(icon, font, Brushes.White,
                (220 - sz.Width) / 2, (140 - sz.Height) / 2);

            _pictureBox.Image = bmp;
        }
    }
}