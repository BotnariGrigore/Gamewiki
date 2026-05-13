using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Helpers;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Forms.Wiki
{
    public class WikiEditorForm : Form
    {
        private readonly int _articleId;
        private readonly ArticleService _articleService = new();
        private readonly CategoryService _categoryService = new();

        private Label lblTitle;
        private Button btnSave;
        private Button btnDelete;
        private CheckedListBox clbCategories;
        private RichTextBox txtContent;
        private TextBox txtTitle;
        private WikiArticle? _article;

        public WikiEditorForm(int articleId)
        {
            _articleId = articleId;
            Text = "Edit Article";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(800, 600);
            MinimumSize = new Size(600, 450);

            InitializeComponents();
            _ = LoadArticleAsync();
        }

        public WikiEditorForm() : this(0)
        {
            Text = "Create Article";
            _articleId = 0;
        }

        private void InitializeComponents()
        {
            var header = new Panel
            {
                Height = 55,
                Dock = DockStyle.Top,
                Padding = new Padding(16, 12, 16, 12)
            };

            lblTitle = ThemeHelper.CreateLabel("Edit Article", 16, FontStyle.Bold, null, 0, 14);
            header.Controls.Add(lblTitle);

            btnSave = ThemeHelper.CreateThemedButton("Save", 0, 14, 120, 30);
            btnSave.Click += async (_, __) => await OnSave();
            header.Controls.Add(btnSave);

            btnDelete = ThemeHelper.CreateThemedButton("Delete", 130, 14, 110, 30);
            btnDelete.Click += OnDelete;
            btnDelete.Visible = _articleId > 0;
            header.Controls.Add(btnDelete);

            Controls.Add(header);

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            Controls.Add(mainPanel);

            // Title row
            var lblArticleTitle = ThemeHelper.CreateLabel("Article Title:", 10, FontStyle.Bold, null, 0, 8);
            mainPanel.Controls.Add(lblArticleTitle);

            txtTitle = new TextBox
            {
                PlaceholderText = "Enter article title...",
                Size = new Size(500, 36)
            };
            var titleWrap = ThemeHelper.WrapInput(txtTitle, 506, 40);
            titleWrap.Location = new Point(0, 32);
            mainPanel.Controls.Add(titleWrap);

            // Category selector on the right
            var lblCat = ThemeHelper.CreateLabel("Categories:", 10, FontStyle.Bold, null, 320, 8);
            mainPanel.Controls.Add(lblCat);

            clbCategories = new CheckedListBox
            {
                Location = new Point(320, 32),
                Size = new Size(250, 150),
                CheckOnClick = true
            };
            mainPanel.Controls.Add(clbCategories);

            // Content
            var lblContent = ThemeHelper.CreateLabel("Content:", 10, FontStyle.Bold, null, 0, 82);
            mainPanel.Controls.Add(lblContent);

            txtContent = new RichTextBox
            {
                Location = new Point(0, 106),
                Size = new Size(mainPanel.Width - 16, 360),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11),
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            mainPanel.Controls.Add(txtContent);

            // Status bar
            var statusBar = new Panel
            {
                Height = 32,
                Dock = DockStyle.Bottom
            };

            var lblStatus = ThemeHelper.CreateLabel("Ready", 9, FontStyle.Regular, null, 10, 8);
            lblStatus.Name = "lblStatus";
            statusBar.Controls.Add(lblStatus);
            Controls.Add(statusBar);

            Resize += (_, __) =>
            {
                if (mainPanel != null)
                {
                    txtContent.Size = new Size(mainPanel.Width - 16, txtContent.Height);
                }
            };
        }

        private async Task LoadArticleAsync()
        {
            if (_articleId <= 0)
            {
                lblTitle.Text = "Create Article";
                btnDelete.Visible = false;
                _ = LoadCategoriesAsync();
                return;
            }

            try
            {
                _article = await _articleService.GetByIdAsync(_articleId);
                if (_article == null)
                {
                    MessageBox.Show("Article not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                lblTitle.Text = "Edit: " + _article.Title;
                txtTitle.Text = _article.Title;
                txtContent.Text = _article.Content;

                await LoadCategoriesAsync();

                var assigned = await _articleService.GetCategoryIdsAsync(_article.ArticleId);
                for (int i = 0; i < clbCategories.Items.Count; i++)
                {
                    var cat = (Category)clbCategories.Items[i];
                    if (assigned.Contains(cat.CategoryId))
                        clbCategories.SetItemChecked(i, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load article: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadCategoriesAsync()
        {
            clbCategories.Items.Clear();
            var cats = await _categoryService.GetAllAsync();
            foreach (var c in cats)
                clbCategories.Items.Add(c);
        }

        private async Task OnSave()
        {
            try
            {
                if (!SessionManager.IsAuthenticated)
                {
                    MessageBox.Show("Please sign in to save.", "Unauthorized", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var title = txtTitle.Text.Trim();
                if (string.IsNullOrEmpty(title))
                {
                    MessageBox.Show("Title is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var content = txtContent.Text.Trim();
                if (string.IsNullOrEmpty(content))
                {
                    MessageBox.Show("Content is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selected = clbCategories.CheckedItems.Cast<Category>().ToList();
                if (!selected.Any())
                {
                    MessageBox.Show("Please select at least one category.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_article == null || _articleId <= 0)
                {
                    var article = new WikiArticle
                    {
                        GameId = selected.First().GameId,
                        AuthorId = SessionManager.CurrentUser!.UserId,
                        Title = title,
                        Slug = SlugGenerator.Generate(title),
                        Content = content,
                        Summary = content.Length > 200 ? content.Substring(0, 200) : content
                    };

                    var id = await _articleService.CreateAsync(article);
                    if (id > 0)
                    {
                        article.ArticleId = id;
                        await _articleService.SetCategoriesAsync(id, selected.Select(c => c.CategoryId).ToList());

                        MessageBox.Show("Article created!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Failed to create article.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    _article.Title = title;
                    _article.Content = content;

                    var ok = await _articleService.UpdateAsync(_article);
                    if (ok)
                    {
                        await _articleService.SetCategoriesAsync(_article.ArticleId, selected.Select(c => c.CategoryId).ToList());
                        MessageBox.Show("Article saved!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Failed to save article.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDelete(object? sender, EventArgs e)
        {
            if (_articleId <= 0) return;

            var confirm = MessageBox.Show("Delete this article? This cannot be undone.",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                _ = DeleteArticleAsync();
            }
        }

        private async Task DeleteArticleAsync()
        {
            try
            {
                var ok = await _articleService.DeleteAsync(_articleId);
                if (ok)
                {
                    MessageBox.Show("Article deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to delete article.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}