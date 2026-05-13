using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Forms.Main
{
    public class CategoryViewForm : Form
    {
        private readonly string _categoryName;
        private readonly CategoryService _categoryService = new();
        private FlowLayoutPanel flpGames;
        private Label lblHeader;

        public CategoryViewForm(string categoryName)
        {
            _categoryName = categoryName;
            Text = "Category: " + categoryName;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(920, 640);

            InitializeComponents();
            _ = LoadGamesAsync();
        }

        private void InitializeComponents()
        {
            lblHeader = new Label { Text = _categoryName, Location = new Point(12, 12), AutoSize = true };
            Controls.Add(lblHeader);

            flpGames = new FlowLayoutPanel { Location = new Point(12, 56), Size = new Size(892, 560), AutoScroll = true };
            Controls.Add(flpGames);
        }

        private async Task LoadGamesAsync()
        {
            try
            {
                flpGames.Controls.Clear();
                var games = (await _categoryService.GetGamesByCategoryNameAsync(_categoryName)).ToList();
                foreach (var g in games)
                {
                    flpGames.Controls.Add(CreateGameCard(g));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load games: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CreateGameCard(Game g)
        {
            var card = new Panel { Size = new Size(420, 180), Margin = new Padding(8) };
            var pb = new PictureBox { Size = new Size(400, 100), Location = new Point(10, 8), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            if (!string.IsNullOrEmpty(g.CoverImage))
            {
                try { pb.Load(g.CoverImage); } catch { pb.Image = null; }
            }
            card.Controls.Add(pb);

            var lbl = new Label { Text = g.Title, Location = new Point(10, 114), Size = new Size(300, 28) };
            card.Controls.Add(lbl);

            var btnView = new Button { Text = "View", Location = new Point(320, 116), Size = new Size(68, 28), UseVisualStyleBackColor = true };
            btnView.Click += (_, __) => { using var v = new GameViewForm(g); v.ShowDialog(this); };
            card.Controls.Add(btnView);

            if (Helpers.SessionManager.IsAuthenticated && Helpers.SessionManager.CurrentUser != null && Helpers.SessionManager.CurrentUser.RoleId == 1)
            {
                var btnRemove = new Button { Text = "Remove", Location = new Point(320, 146), Size = new Size(68, 28), UseVisualStyleBackColor = true };
                btnRemove.Click += async (_, __) =>
                {
                    var ok = MessageBox.Show($"Remove '{g.Title}' from category '{_categoryName}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (ok == DialogResult.Yes)
                    {
                        var res = await _categoryService.RemoveCategoryFromGameAsync(_categoryName, g.GameId);
                        if (res) await LoadGamesAsync();
                        else MessageBox.Show("Failed to remove game from category.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                card.Controls.Add(btnRemove);
            }

            return card;
        }
    }
}