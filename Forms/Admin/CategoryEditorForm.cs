using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Helpers;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Forms.Admin
{
    public class CategoryEditorForm : Form
    {
        private readonly CategoryService _categoryService = new();
        private readonly GameService _gameService = new();

        private FlowLayoutPanel flpCategories;
        private Button btnCreate;
        private Label lblTitle;

        public CategoryEditorForm()
        {
            Text = "Category Management";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(960, 680);
            MinimumSize = new Size(700, 500);

            InitializeComponents();
            _ = LoadDataAsync();
        }

        private void InitializeComponents()
        {
            var header = new Panel
            {
                Height = 64,
                Dock = DockStyle.Top,
                Padding = new Padding(20, 14, 20, 14)
            };

            lblTitle = ThemeHelper.CreateLabel("Category Management", 16, FontStyle.Bold, null, 0, 14);
            header.Controls.Add(lblTitle);

            btnCreate = ThemeHelper.CreateThemedButton("Create Category", 0, 12, 160, 36);
            btnCreate.Click += (_, __) => OpenCreateDialog();
            header.Controls.Add(btnCreate);

            Controls.Add(header);

            flpCategories = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            Controls.Add(flpCategories);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                flpCategories.Controls.Clear();
                var list = (await _categoryService.GetPopularCategoriesAsync()).ToList();

                if (!list.Any())
                {
                    flpCategories.Controls.Add(new Label
                    {
                        Text = "No categories defined yet. Click 'Create Category' to get started.",
                        AutoSize = true,
                        Margin = new Padding(20)
                    });
                    return;
                }

                foreach (var pc in list)
                {
                    flpCategories.Controls.Add(CreateCategoryCard(pc));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load categories: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CreateCategoryCard(PopularCategory pc)
        {
            var card = ThemeHelper.CreateCardPanel(600, 90);

            var lblName = ThemeHelper.CreateLabel(pc.CategoryName, 13, FontStyle.Bold, null, 14, 8);
            card.Controls.Add(lblName);

            var lblCount = ThemeHelper.CreateLabel($"{pc.GameCount} games", 10, FontStyle.Bold, null, 14, 36);
            card.Controls.Add(lblCount);

            var btnView = ThemeHelper.CreateThemedButton("View Games", card.Width - 240, 6, 100, 30);
            btnView.Click += (_, __) =>
            {
                using var v = new Main.CategoryViewForm(pc.CategoryName);
                v.ShowDialog(this);
                _ = LoadDataAsync();
            };
            card.Controls.Add(btnView);

            var btnEdit = ThemeHelper.CreateThemedButton("Assign Games", card.Width - 120, 6, 100, 30);
            btnEdit.Click += (_, __) => OpenAssignDialog(pc.CategoryName);
            card.Controls.Add(btnEdit);

            var btnDelete = ThemeHelper.CreateThemedButton("Delete", card.Width - 16, 50, 80, 28);
            btnDelete.Click += async (_, __) =>
            {
                var confirm = MessageBox.Show($"Delete category '{pc.CategoryName}' from all games?",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        var games = await _gameService.GetAllAsync();
                        foreach (var g in games)
                        {
                            await _categoryService.RemoveCategoryFromGameAsync(pc.CategoryName, g.GameId);
                        }
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to delete: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            card.Controls.Add(btnDelete);

            return card;
        }

        private void OpenCreateDialog()
        {
            using var dlg = new CreateCategoryDialog(_gameService);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var (name, desc, gameIds) = dlg.Result;
                _ = CreateCategoryAsync(name, desc, gameIds);
            }
        }

        private void OpenAssignDialog(string categoryName)
        {
            using var dlg = new CreateCategoryDialog(_gameService, categoryName);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var (name, desc, gameIds) = dlg.Result;
                _ = CreateCategoryAsync(name, desc, gameIds);
            }
        }

        private async Task CreateCategoryAsync(string name, string? desc, IEnumerable<int> gameIds)
        {
            try
            {
                var ok = await _categoryService.AddCategoryToGamesAsync(name, desc, gameIds);
                if (ok)
                {
                    await LoadDataAsync();
                    MessageBox.Show($"Category '{name}' assigned to {gameIds.Count()} game(s).",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                    MessageBox.Show("Failed to create category.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class CreateCategoryDialog : Form
        {
            private readonly GameService _gameService;
            private TextBox txtName;
            private TextBox txtDesc;
            private CheckedListBox clbGames;
            private Button btnOk;
            private Button btnCancel;
            private Label lblStatus;

            public (string Name, string? Description, List<int> GameIds) Result { get; private set; }

            public CreateCategoryDialog(GameService gameService, string? existingName = null)
            {
                _gameService = gameService;
                Text = existingName != null ? "Assign Category to Games" : "Create Category";
                StartPosition = FormStartPosition.CenterParent;
                Size = new Size(640, 560);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;

                InitializeComponents(existingName);
                _ = LoadGamesAsync();
            }

            private void InitializeComponents(string? existingName)
            {
                var header = new Panel
                {
                    Height = 50,
                    Dock = DockStyle.Top
                };
                Controls.Add(header);

                var title = ThemeHelper.CreateLabel(
                    existingName != null ? $"Assign Games to: '{existingName}'" : "Create New Category",
                    14, FontStyle.Bold, null, 14, 14);
                header.Controls.Add(title);

                var main = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(16)
                };
                Controls.Add(main);

                int y = 8;

                if (existingName == null)
                {
                    main.Controls.Add(ThemeHelper.CreateLabel("Category Name:", 10, FontStyle.Bold, null, 0, y));
                    txtName = new TextBox { PlaceholderText = "Enter category name...", Size = new Size(500, 36) };
                    var wrap = ThemeHelper.WrapInput(txtName, 506, 40);
                    wrap.Location = new Point(0, y + 22);
                    main.Controls.Add(wrap);
                    y += 72;
                }
                else
                {
                    txtName = new TextBox { Text = existingName, Enabled = false };
                    y = -10;
                }

                main.Controls.Add(ThemeHelper.CreateLabel("Description (optional):", 10, FontStyle.Bold, null, 0, y));
                y += 24;

                txtDesc = new TextBox { Size = new Size(500, 80), Multiline = true, ScrollBars = ScrollBars.Vertical };
                var wrapDesc = ThemeHelper.WrapInput(txtDesc, 506, 84);
                wrapDesc.Location = new Point(0, y + 2);
                main.Controls.Add(wrapDesc);
                y += 96;

                main.Controls.Add(ThemeHelper.CreateLabel("Select games to include:", 10, FontStyle.Bold, null, 0, y));
                y += 24;

                clbGames = new CheckedListBox
                {
                    Location = new Point(0, y),
                    Size = new Size(506, 250),
                    CheckOnClick = true
                };
                main.Controls.Add(clbGames);
                y += 260;

                lblStatus = ThemeHelper.CreateLabel("", 9, FontStyle.Regular, null, 0, y + 5);
                main.Controls.Add(lblStatus);

                var btnPanel = new Panel { Height = 40, Dock = DockStyle.Bottom };

                btnOk = ThemeHelper.CreateThemedButton("OK", 10, 6, 100, 30);
                btnOk.Click += (_, __) => OnOk();
                btnPanel.Controls.Add(btnOk);

                btnCancel = ThemeHelper.CreateThemedButton("Cancel", 120, 6, 100, 30);
                btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };
                btnPanel.Controls.Add(btnCancel);

                // Select all toggle
                var btnSelectAll = ThemeHelper.CreateThemedButton("Select All", 230, 6, 100, 30);
                btnSelectAll.Click += (_, __) =>
                {
                    for (int i = 0; i < clbGames.Items.Count; i++)
                        clbGames.SetItemChecked(i, true);
                };
                btnPanel.Controls.Add(btnSelectAll);

                Controls.Add(btnPanel);
            }

            private async Task LoadGamesAsync()
            {
                try
                {
                    var games = await _gameService.GetAllAsync();
                    foreach (var g in games)
                        clbGames.Items.Add(new GameListItem(g), true);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Error loading games: " + ex.Message;
                }
            }

            private void OnOk()
            {
                if (txtName.Text.Trim().Length == 0)
                {
                    lblStatus.Text = "Category name cannot be empty.";
                    return;
                }

                var selected = clbGames.CheckedItems.Cast<GameListItem>().Select(x => x.Game.GameId).ToList();
                if (!selected.Any())
                {
                    lblStatus.Text = "Please select at least one game.";
                    return;
                }

                Result = (txtName.Text.Trim(),
                    string.IsNullOrWhiteSpace(txtDesc.Text) ? null : txtDesc.Text.Trim(),
                    selected);
                DialogResult = DialogResult.OK;
                Close();
            }

            private class GameListItem
            {
                public Game Game { get; }
                public GameListItem(Game g) { Game = g; }
                public override string ToString() => Game.Title + $" (ID: {Game.GameId})";
            }
        }
    }
}