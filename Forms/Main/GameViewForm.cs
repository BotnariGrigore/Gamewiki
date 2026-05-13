using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Helpers;
using GameWikiApp.Services;
using GameWikiApp.Models;
using GameWikiApp.Data;

namespace GameWikiApp.Forms.Main
{
    public class GameViewForm : Form
    {
        private readonly int _gameId;
        private readonly GameService _gameService = new();
        private readonly ArticleService _articleService = new();
        private readonly CategoryService _categoryService = new();

        private PictureBox pbBanner;
        private Label lblTitle;
        private Label lblDescription;
        private Label lblCategories;
        private FlowLayoutPanel flpArticles;

        public GameViewForm(int gameId)
        {
            _gameId = gameId;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1000, 750);
            MinimumSize = new Size(700, 550);

            InitializeLayout();
            _ = LoadContentAsync();
        }

        public GameViewForm(Game game) : this(game.GameId) { }

        private void InitializeLayout()
        {
            Text = "Game Details";

            var mainScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            Controls.Add(mainScroll);

            // Banner
            pbBanner = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(mainScroll.Width, 260),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainScroll.Controls.Add(pbBanner);

            // Title
            lblTitle = new Label
            {
                Location = new Point(20, 270),
                Size = new Size(mainScroll.Width - 40, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainScroll.Controls.Add(lblTitle);

            // Description
            lblDescription = new Label
            {
                Location = new Point(20, 320),
                Size = new Size(mainScroll.Width - 40, 80)
            };
            mainScroll.Controls.Add(lblDescription);

            // Categories
            lblCategories = ThemeHelper.CreateLabel("", 10, FontStyle.Regular, null, 20, 410);
            mainScroll.Controls.Add(lblCategories);

            var sep = ThemeHelper.CreateSeparator(mainScroll.Width - 40, 20, 435);
            mainScroll.Controls.Add(sep);

            // Articles section
            var lblArticles = ThemeHelper.CreateLabel("Wiki Articles", 16, FontStyle.Bold, null, 20, 450);
            mainScroll.Controls.Add(lblArticles);

            flpArticles = new FlowLayoutPanel
            {
                Location = new Point(20, 480),
                Size = new Size(mainScroll.Width - 40, 200),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            mainScroll.Controls.Add(flpArticles);

            Resize += (_, __) =>
            {
                pbBanner.Size = new Size(mainScroll.Width, 260);
                lblTitle.Size = new Size(mainScroll.Width - 40, 40);
                lblDescription.Size = new Size(mainScroll.Width - 40, 80);
                sep.Width = mainScroll.Width - 40;
                flpArticles.Size = new Size(mainScroll.Width - 40, mainScroll.Height - flpArticles.Top - 20);
            };
        }

        private async Task LoadContentAsync()
        {
            try
            {
                var game = await _gameService.GetByIdAsync(_gameId);
                if (game == null)
                {
                    MessageBox.Show("Game not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                lblTitle.Text = game.Title;
                lblDescription.Text = game.ShortDescription ?? game.FullDescription ?? "No description available.";

                if (!string.IsNullOrEmpty(game.BannerImage))
                {
                    try
                    {
                        var path = game.BannerImage.Replace('/', Path.DirectorySeparatorChar);
                        var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                        if (File.Exists(local))
                            pbBanner.Image = Image.FromFile(local);
                        else
                            pbBanner.Image = null;
                    }
                    catch { pbBanner.Image = null; }
                }

                // Load categories
                var categories = await _categoryService.GetByGameIdAsync(_gameId);
                if (categories.Any())
                {
                    lblCategories.Text = string.Join(" · ", categories.Select(c => c.CategoryName));
                }
                else
                {
                    lblCategories.Text = "No categories assigned";
                }

                // Load wiki articles for this game
                var articles = (await _articleService.SearchAsync("")).Where(a => a.GameId == _gameId).ToList();
                foreach (var a in articles)
                {
                    var card = ThemeHelper.CreateCardPanel(flpArticles.Width - 10, 60);

                    var lblArtTitle = ThemeHelper.CreateLabel(a.Title, 10.5f, FontStyle.Bold, null, 10, 8);
                    lblArtTitle.Cursor = Cursors.Hand;
                    lblArtTitle.Click += (_, __) =>
                    {
                        using var v = new ArticleViewForm(a.ArticleId);
                        v.ShowDialog(this);
                    };
                    card.Controls.Add(lblArtTitle);

                    var summary = (a.Summary ?? "").Trim();
                    if (summary.Length > 80) summary = summary.Substring(0, 80) + "...";
                    var lblArtSummary = ThemeHelper.CreateLabel(summary, 8.5f, FontStyle.Regular, null, 10, 30);
                    card.Controls.Add(lblArtSummary);

                    flpArticles.Controls.Add(card);
                }

                if (!articles.Any())
                {
                    flpArticles.Controls.Add(new Label
                    {
                        Text = "No wiki articles yet for this game.",
                        AutoSize = true,
                        Margin = new Padding(12)
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load game: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}