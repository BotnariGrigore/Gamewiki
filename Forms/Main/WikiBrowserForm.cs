using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Helpers;
using GameWikiApp.Services;
using GameWikiApp.Models;
using GameWikiApp.Data;
using GameWikiApp.Forms.Wiki;

namespace GameWikiApp.Forms.Main
{
    public class WikiBrowserForm : Form
    {
        private Panel sidebar;
        private Panel mainContent;
        private FlowLayoutPanel flpArticles;
        private TextBox txtSearch;
        private Label lblTitle;
        private ComboBox cmbSortBy;
        private ComboBox cmbFilterCategory;
        private ListBox lstCategories;

        private readonly ArticleService _articleService = new();
        private readonly CategoryService _categoryService = new();
        private readonly GameService _gameService = new();

        private List<WikiArticle> _allArticles = new();
        private List<Category> _allCategories = new();

        public WikiBrowserForm()
        {
            Text = "Wiki Browser";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1100, 750);
            MinimumSize = new Size(800, 600);

            InitializeLayout();
            _ = LoadDataAsync();
        }

        private void InitializeLayout()
        {
            // Left sidebar with categories
            sidebar = new Panel
            {
                Width = 260,
                Dock = DockStyle.Left,
                Padding = new Padding(16)
            };
            Controls.Add(sidebar);

            int yPos = 0;

            // Categories title
            var lblCatTitle = ThemeHelper.CreateLabel("Categories", 12, FontStyle.Bold, null, 0, yPos);
            sidebar.Controls.Add(lblCatTitle);
            yPos = lblCatTitle.Bottom + 12;

            // Search box
            txtSearch = new TextBox
            {
                PlaceholderText = "Search articles...",
                Size = new Size(236, 40)
            };
            var searchWrapper = ThemeHelper.WrapInput(txtSearch, 240, 46);
            searchWrapper.Location = new Point(0, yPos);
            sidebar.Controls.Add(searchWrapper);
            yPos = searchWrapper.Bottom + 12;

            // Categories list
            lstCategories = new ListBox
            {
                Size = new Size(236, 400),
                BorderStyle = BorderStyle.FixedSingle
            };
            lstCategories.SelectedIndexChanged += async (_, __) => await FilterArticles();
            lstCategories.Location = new Point(0, yPos);
            sidebar.Controls.Add(lstCategories);
            yPos = lstCategories.Bottom + 12;

            // "All Articles" button
            var btnAll = ThemeHelper.CreateThemedButton("All Articles", 0, yPos, 120, 36);
            btnAll.Click += async (_, __) => { lstCategories.ClearSelected(); await FilterArticles(); };
            sidebar.Controls.Add(btnAll);

            // Divider
            var divider = new Panel
            {
                Width = 1,
                Dock = DockStyle.Left
            };
            Controls.Add(divider);

            // Main content area
            mainContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24)
            };
            Controls.Add(mainContent);

            // Header bar
            var headerBar = new Panel
            {
                Height = 56,
                Dock = DockStyle.Top,
                Padding = new Padding(16, 8, 16, 8)
            };
            mainContent.Controls.Add(headerBar);

            // Title on the left
            lblTitle = ThemeHelper.CreateLabel("Wiki Articles", 16, FontStyle.Bold, null, 0, 14);
            headerBar.Controls.Add(lblTitle);

            // Controls panel on the right side
            var controlsPanel = new Panel
            {
                Location = new Point(headerBar.Width / 2, 8),
                Size = new Size(headerBar.Width / 2 - 20, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // Sort dropdown
            var lblSort = ThemeHelper.CreateLabel("Sort:", 8.5f, FontStyle.Regular, null, 0, 6);
            controlsPanel.Controls.Add(lblSort);

            cmbSortBy = new ComboBox
            {
                Location = new Point(38, 2),
                Size = new Size(130, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSortBy.Items.AddRange(new[] { "Newest First", "Oldest First", "Title A-Z", "Most Viewed" });
            cmbSortBy.SelectedIndex = 0;
            cmbSortBy.SelectedIndexChanged += async (_, __) => await FilterArticles();
            controlsPanel.Controls.Add(cmbSortBy);

            // Category filter
            var lblFilter = ThemeHelper.CreateLabel("Category:", 8.5f, FontStyle.Regular, null, 178, 6);
            controlsPanel.Controls.Add(lblFilter);

            cmbFilterCategory = new ComboBox
            {
                Location = new Point(248, 2),
                Size = new Size(140, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFilterCategory.Items.Add("All Categories");
            cmbFilterCategory.SelectedIndex = 0;
            cmbFilterCategory.SelectedIndexChanged += async (_, __) => await FilterArticles();
            controlsPanel.Controls.Add(cmbFilterCategory);

            // New Article button
            var btnNewArticle = ThemeHelper.CreateThemedButton("+ New Article", 400, 2, 110, 30);
            btnNewArticle.Click += (_, __) => OpenCreateArticle();
            controlsPanel.Controls.Add(btnNewArticle);

            headerBar.Controls.Add(controlsPanel);

            // Articles panel
            flpArticles = new FlowLayoutPanel
            {
                Location = new Point(24, 70),
                Size = new Size(mainContent.Width - 48, mainContent.Height - 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            mainContent.Controls.Add(flpArticles);

            Resize += (_, __) =>
            {
                flpArticles.Size = new Size(mainContent.Width - 48, mainContent.Height - 100);
            };
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _allArticles = (await _articleService.SearchAsync("")).ToList();
                _allCategories = (await _categoryService.GetAllAsync()).ToList();

                lstCategories.Items.Clear();
                cmbFilterCategory.Items.Clear();
                cmbFilterCategory.Items.Add("All Categories");

                var categoryGroups = _allCategories.GroupBy(c => c.CategoryName).Select(g => g.Key).OrderBy(n => n);
                foreach (var cat in categoryGroups)
                {
                    lstCategories.Items.Add(cat);
                    cmbFilterCategory.Items.Add(cat);
                }

                cmbFilterCategory.SelectedIndex = 0;

                await FilterArticles();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load wiki data: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task FilterArticles()
        {
            try
            {
                var query = txtSearch.Text.Trim().ToLower();
                var selectedCat = lstCategories.SelectedItem?.ToString();
                var filterCat = cmbFilterCategory.SelectedItem?.ToString();
                if (filterCat == "All Categories") filterCat = null;

                var filtered = _allArticles.AsEnumerable();

                if (!string.IsNullOrEmpty(query))
                    filtered = filtered.Where(a =>
                        a.Title.ToLower().Contains(query) ||
                        (a.Summary ?? "").ToLower().Contains(query));

                if (!string.IsNullOrEmpty(selectedCat))
                    filtered = filtered.Where(a =>
                        _allCategories.Any(c => c.CategoryName == selectedCat && c.GameId == a.GameId));
                else if (!string.IsNullOrEmpty(filterCat))
                    filtered = filtered.Where(a =>
                        _allCategories.Any(c => c.CategoryName == filterCat && c.GameId == a.GameId));

                switch (cmbSortBy.SelectedIndex)
                {
                    case 1: filtered = filtered.OrderBy(a => a.CreatedAt); break;
                    case 2: filtered = filtered.OrderBy(a => a.Title); break;
                    case 3: filtered = filtered.OrderByDescending(a => a.ViewsCount); break;
                    default: filtered = filtered.OrderByDescending(a => a.CreatedAt); break;
                }

                flpArticles.Controls.Clear();
                var articles = filtered.ToList();
                lblTitle.Text = $"Wiki Articles ({articles.Count})";

                foreach (var a in articles)
                {
                    flpArticles.Controls.Add(CreateArticleCard(a));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Filter failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CreateArticleCard(WikiArticle a)
        {
            var card = ThemeHelper.CreateCardPanel(400, 200);

            // Title
            var lblTitle = ThemeHelper.CreateLabel(a.Title, 11, FontStyle.Bold, null, 12, 8);
            lblTitle.Size = new Size(card.Width - 100, 40);
            lblTitle.MaximumSize = new Size(card.Width - 100, 40);
            card.Controls.Add(lblTitle);

            // Game label
            var lblGame = ThemeHelper.CreateLabel($"Game ID: {a.GameId}", 8.5f, FontStyle.Regular, null, 12, 40);
            card.Controls.Add(lblGame);

            // Summary
            var summary = (a.Summary ?? "No summary").Trim();
            if (summary.Length > 100) summary = summary.Substring(0, 100) + "...";
            var lblSummary = new Label
            {
                Text = summary,
                Location = new Point(12, 60),
                Size = new Size(card.Width - 36, 60),
                MaximumSize = new Size(card.Width - 36, 60)
            };
            card.Controls.Add(lblSummary);

            // Views
            var lblViews = ThemeHelper.CreateLabel($"{a.ViewsCount} views", 8.5f, FontStyle.Regular, null, 12, 160);
            card.Controls.Add(lblViews);

            // View button
            var btnView = ThemeHelper.CreateThemedButton("View", card.Width - 100, 168, 70, 28);
            btnView.Click += (_, __) =>
            {
                using var v = new ArticleViewForm(a.ArticleId);
                v.ShowDialog(this);
            };
            card.Controls.Add(btnView);

            return card;
        }

        private void OpenCreateArticle()
        {
            using var editor = new WikiEditorForm();
            editor.ShowDialog(this);
            _ = LoadDataAsync();
        }
    }
}