using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Helpers;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Forms.Admin
{
    public class GameEditorForm : Form
    {
        private TextBox txtTitle;
        private RichTextBox txtShort;
        private RichTextBox txtFull;
        private TextBox txtCover;
        private Button btnSave;
        private Button btnBrowse;
        private PictureBox pbCoverPreview;
        private readonly GameService _gameService = new();

        public GameEditorForm()
        {
            Text = "Create Game";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(640, 520);
            MinimumSize = new Size(500, 400);

            InitializeComponents();
        }

        public GameEditorForm(int gameId) : this()
        {
            _ = LoadGameAsync(gameId);
        }

        private void InitializeComponents()
        {
            var header = new Panel
            {
                Height = 55,
                Dock = DockStyle.Top,
                Padding = new Padding(16, 10, 16, 10)
            };

            var lblTitleHeader = ThemeHelper.CreateLabel("Create Game", 16, FontStyle.Bold, null, 0, 14);
            header.Controls.Add(lblTitleHeader);
            Controls.Add(header);

            var main = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            Controls.Add(main);

            int y = 0;

            // Title
            main.Controls.Add(ThemeHelper.CreateLabel("Title:", 10, FontStyle.Bold, null, 0, y));
            y += 22;
            txtTitle = new TextBox
            {
                PlaceholderText = "Game title...",
                Size = new Size(main.Width - 48, 38)
            };
            var wrapTitle = ThemeHelper.WrapInput(txtTitle, main.Width - 44, 42);
            wrapTitle.Location = new Point(0, y);
            main.Controls.Add(wrapTitle);
            y += 52;

            // Short Description
            main.Controls.Add(ThemeHelper.CreateLabel("Short Description:", 10, FontStyle.Bold, null, 0, y));
            y += 22;
            txtShort = new RichTextBox
            {
                Size = new Size(main.Width - 48, 70),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true
            };
            var wrapShort = ThemeHelper.WrapInput(txtShort, main.Width - 44, 74);
            wrapShort.Location = new Point(0, y);
            main.Controls.Add(wrapShort);
            y += 84;

            // Full Description
            main.Controls.Add(ThemeHelper.CreateLabel("Full Description:", 10, FontStyle.Bold, null, 0, y));
            y += 22;
            txtFull = new RichTextBox
            {
                Size = new Size(main.Width - 48, 120),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true
            };
            var wrapFull = ThemeHelper.WrapInput(txtFull, main.Width - 44, 124);
            wrapFull.Location = new Point(0, y);
            main.Controls.Add(wrapFull);
            y += 134;

            // Cover image
            main.Controls.Add(ThemeHelper.CreateLabel("Cover Image URL:", 10, FontStyle.Bold, null, 0, y));
            y += 22;

            var coverRow = new Panel { Height = 44, Dock = DockStyle.Top };
            txtCover = new TextBox
            {
                PlaceholderText = "Image URL or local path...",
                Size = new Size(main.Width - 160, 40)
            };
            var wrapCover = ThemeHelper.WrapInput(txtCover, main.Width - 156, 44);
            wrapCover.Dock = DockStyle.Left;
            coverRow.Controls.Add(wrapCover);

            btnBrowse = ThemeHelper.CreateThemedButton("Browse", 0, 2, 90, 40);
            btnBrowse.Click += BtnBrowse_Click;
            coverRow.Controls.Add(btnBrowse);

            coverRow.Location = new Point(0, txtFull.Bottom + 36);
            main.Controls.Add(coverRow);
            y += 54;

            // Cover preview
            pbCoverPreview = new PictureBox
            {
                Size = new Size(120, 80),
                Location = new Point(0, txtCover.Bottom + 8),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.None
            };
            main.Controls.Add(pbCoverPreview);

            txtCover.TextChanged += (_, __) =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(txtCover.Text))
                    {
                        var path = txtCover.Text.Replace('/', Path.DirectorySeparatorChar);
                        var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                        if (File.Exists(local))
                            pbCoverPreview.Image = Image.FromFile(local);
                        else
                            pbCoverPreview.Image = null;
                    }
                    else
                    {
                        pbCoverPreview.Image = null;
                    }
                }
                catch { pbCoverPreview.Image = null; }
            };

            // Status bar
            var statusBar = new Panel { Height = 32, Dock = DockStyle.Bottom };
            var lblStatus = ThemeHelper.CreateLabel("Ready", 9, FontStyle.Regular, null, 10, 8);
            lblStatus.Name = "lblStatus";
            statusBar.Controls.Add(lblStatus);
            Controls.Add(statusBar);

            // Save button
            btnSave = ThemeHelper.CreateThemedButton("Save Game", main.Width - 16, txtCover.Bottom + 8, 140, 40);
            btnSave.Click += async (_, __) => await OnSaveAsync();
            main.Controls.Add(btnSave);

            Resize += (_, __) =>
            {
                Control[] controls = { txtTitle, txtShort, txtFull, txtCover };
                foreach (var c in controls)
                    if (c != null && c.Parent != null)
                        c.Width = main.Width - 52;
            };
        }

        private async Task LoadGameAsync(int gameId)
        {
            try
            {
                var game = await _gameService.GetByIdAsync(gameId);
                if (game == null)
                {
                    MessageBox.Show("Game not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                Text = "Edit Game";
                lblStatus("Editing: " + game.Title);
                txtTitle.Text = game.Title;
                txtShort.Text = game.ShortDescription ?? "";
                txtFull.Text = game.FullDescription ?? "";
                txtCover.Text = game.CoverImage ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load game: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lblStatus(string text)
        {
            var ctrl = Controls.Find("lblStatus", true);
            if (ctrl.Length > 0) ctrl[0].Text = text;
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.webp|All Files|*.*"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");
                Directory.CreateDirectory(imagesDir);
                var destFileName = Path.GetFileName(ofd.FileName);
                var destPath = Path.Combine(imagesDir, destFileName);
                int i = 1;
                while (File.Exists(destPath))
                {
                    var name = Path.GetFileNameWithoutExtension(destFileName);
                    var ext = Path.GetExtension(destFileName);
                    destFileName = $"{name}_{i}{ext}";
                    destPath = Path.Combine(imagesDir, destFileName);
                    i++;
                }
                File.Copy(ofd.FileName, destPath);
                txtCover.Text = Path.Combine("Assets", "Images", destFileName).Replace("\\", "/");
                MessageBox.Show($"Image copied to: {txtCover.Text}", "Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to copy image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnSaveAsync()
        {
            try
            {
                if (!SessionManager.IsAuthenticated)
                {
                    MessageBox.Show("You must be signed in.", "Unauthorized", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var user = SessionManager.CurrentUser!;
                if (user.RoleId != 1)
                {
                    MessageBox.Show("Only admins can create/edit games.", "Forbidden", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var title = txtTitle.Text.Trim();
                if (string.IsNullOrEmpty(title))
                {
                    MessageBox.Show("Title is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var game = new Game
                {
                    CreatedBy = user.UserId,
                    Title = title,
                    Slug = SlugGenerator.Generate(title),
                    ShortDescription = txtShort.Text.Trim(),
                    FullDescription = txtFull.Text.Trim(),
                    CoverImage = txtCover.Text.Trim()
                };

                var id = await _gameService.CreateAsync(game);
                if (id > 0)
                {
                    MessageBox.Show("Game created!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to create game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}