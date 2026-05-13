using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Data;
using GameWikiApp.Helpers;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Forms.Admin
{
    public class AdminDashboardForm : Form
    {
        private TabControl tabControl;

        // Users tab
        private DataGridView dgvUsers;
        private Button btnExport;
        private UserRepository _userRepo = new();

        // Games tab
        private DataGridView dgvGames;
        private GameService _gameService = new();

        // Articles tab
        private DataGridView dgvArticles;
        private ArticleService _articleService = new();

        // Notifications tab
        private FlowLayoutPanel flpAdminNotifs;
        private NotificationService _notificationService = new();

        public AdminDashboardForm()
        {
            Text = "Admin Dashboard";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1000, 700);
            MinimumSize = new Size(800, 500);

            InitializeLayout();
            _ = LoadDataAsync();
        }

        private void InitializeLayout()
        {
            var header = new Panel
            {
                Height = 64,
                Dock = DockStyle.Top,
                Padding = new Padding(20, 14, 20, 14)
            };
            Controls.Add(header);

            var lblTitle = ThemeHelper.CreateLabel("Admin Dashboard", 18, FontStyle.Bold, null, 0, 12);
            header.Controls.Add(lblTitle);

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // ── Tab 1: Users ──
            var tabUsers = new TabPage("Users");

            var pnlUsers = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };
            tabUsers.Controls.Add(pnlUsers);

            var btnRow1 = new Panel { Height = 36, Dock = DockStyle.Top };
            pnlUsers.Controls.Add(btnRow1);

            btnExport = ThemeHelper.CreateThemedButton("Export CSV", 0, 4, 140, 30);
            btnExport.Click += async (_, __) => await ExportCsvAsync();
            btnRow1.Controls.Add(btnExport);

            var btnRefreshUsers = ThemeHelper.CreateThemedButton("Refresh", 155, 4, 100, 30);
            btnRefreshUsers.Click += async (_, __) => await LoadUsersAsync();
            btnRow1.Controls.Add(btnRefreshUsers);

            dgvUsers = new DataGridView
            {
                Location = new Point(0, 42),
                Size = new Size(pnlUsers.Width, pnlUsers.Height - 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            dgvUsers.EnableHeadersVisualStyles = false;

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserId", HeaderText = "ID", DataPropertyName = "UserId", Width = 60, ReadOnly = true });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Username", HeaderText = "Username", DataPropertyName = "Username", Width = 170, ReadOnly = true });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", DataPropertyName = "Email", Width = 220, ReadOnly = true });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Role", HeaderText = "Role", DataPropertyName = "Role", Width = 90, ReadOnly = true });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Created", HeaderText = "Created", DataPropertyName = "CreatedAt", Width = 150, ReadOnly = true });

            var colToggle = new DataGridViewButtonColumn { Name = "ToggleRole", HeaderText = "", Text = "Toggle Role", UseColumnTextForButtonValue = true, Width = 110 };
            dgvUsers.Columns.Add(colToggle);

            var colDel = new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "Delete", UseColumnTextForButtonValue = true, Width = 90 };
            dgvUsers.Columns.Add(colDel);

            dgvUsers.CellContentClick += async (s, e) => await OnUserGridClick(s, e);
            pnlUsers.Controls.Add(dgvUsers);

            // ── Tab 2: Games ──
            var tabGames = new TabPage("Games");

            var pnlGames = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };
            tabGames.Controls.Add(pnlGames);

            var btnRow2 = new Panel { Height = 36, Dock = DockStyle.Top };
            pnlGames.Controls.Add(btnRow2);

            var btnAddGame = ThemeHelper.CreateThemedButton("Add Game", 0, 4, 120, 30);
            btnAddGame.Click += (_, __) =>
            {
                using var f = new GameEditorForm();
                f.ShowDialog(this);
                _ = LoadGamesAsync();
            };
            btnRow2.Controls.Add(btnAddGame);

            dgvGames = new DataGridView
            {
                Location = new Point(0, 42),
                Size = new Size(pnlGames.Width, pnlGames.Height - 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            dgvGames.EnableHeadersVisualStyles = false;

            dgvGames.Columns.Add(new DataGridViewTextBoxColumn { Name = "GameId", HeaderText = "ID", DataPropertyName = "GameId", Width = 60 });
            dgvGames.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Title", DataPropertyName = "Title", Width = 250 });
            dgvGames.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "Created", DataPropertyName = "CreatedAt", Width = 150 });
            dgvGames.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy", HeaderText = "Author ID", DataPropertyName = "CreatedBy", Width = 90 });

            var colDelGame = new DataGridViewButtonColumn { Name = "DeleteGame", HeaderText = "", Text = "Delete", UseColumnTextForButtonValue = true, Width = 50 };
            dgvGames.Columns.Add(colDelGame);

            dgvGames.CellContentClick += async (s, e) => await OnGameGridClick(s, e);
            pnlGames.Controls.Add(dgvGames);

            // ── Tab 3: Notifications ──
            var tabNotifs = new TabPage("System Notifications");

            var pnlNotifs = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };
            tabNotifs.Controls.Add(pnlNotifs);

            var btnRefreshNotifs = ThemeHelper.CreateThemedButton("Refresh", 0, 8, 120, 30);
            btnRefreshNotifs.Click += async (_, __) => await LoadNotificationsAsync();
            pnlNotifs.Controls.Add(btnRefreshNotifs);

            flpAdminNotifs = new FlowLayoutPanel
            {
                Location = new Point(0, 46),
                Size = new Size(pnlNotifs.Width, pnlNotifs.Height - 52),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            pnlNotifs.Controls.Add(flpAdminNotifs);

            // Add tabs
            tabControl.TabPages.Add(tabUsers);
            tabControl.TabPages.Add(tabGames);
            tabControl.TabPages.Add(tabNotifs);

            Controls.Add(tabControl);

            Resize += (_, __) =>
            {
                dgvUsers.Size = new Size(pnlUsers.Width, pnlUsers.Height - 48);
                dgvGames.Size = new Size(pnlGames.Width, pnlGames.Height - 48);
                flpAdminNotifs.Size = new Size(pnlNotifs.Width, pnlNotifs.Height - 52);
            };
        }

        private async Task LoadDataAsync()
        {
            await LoadUsersAsync();
            await LoadGamesAsync();
            await LoadNotificationsAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var list = (await _userRepo.GetAllAsync()).ToList();
                dgvUsers.DataSource = list.Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    Role = u.RoleId == 1 ? "Admin" : "User",
                    CreatedAt = u.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed loading users: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnUserGridClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = (DataGridView)sender;
            var row = grid.Rows[e.RowIndex];

            if (grid.Columns[e.ColumnIndex].Name == "ToggleRole")
            {
                try
                {
                    var id = Convert.ToInt32(row.Cells["UserId"].Value);
                    var currentRole = row.Cells["Role"].Value?.ToString()?.Contains("Admin") == true ? 1 : 2;
                    var newRole = currentRole == 1 ? 2 : 1;
                    var ok = await _userRepo.UpdateRoleAsync(id, newRole);
                    if (ok)
                    {
                        await LoadUsersAsync();
                        MessageBox.Show($"Role updated for user #{id}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to change role: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (grid.Columns[e.ColumnIndex].Name == "Delete")
            {
                try
                {
                    var id = Convert.ToInt32(row.Cells["UserId"].Value);
                    var username = row.Cells["Username"].Value?.ToString() ?? "";
                    var result = MessageBox.Show($"Delete user '{username}' (ID {id})?\nThis will also delete their articles and comments.\nThis action cannot be undone.",
                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        var ok = await _userRepo.DeleteAsync(id);
                        if (ok) await LoadUsersAsync();
                        else MessageBox.Show("Failed to delete user.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to delete user: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task LoadGamesAsync()
        {
            try
            {
                var games = await _gameService.GetAllAsync();
                dgvGames.DataSource = games.Select(g => new
                {
                    g.GameId,
                    g.Title,
                    CreatedAt = g.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    g.CreatedBy
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed loading games: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnGameGridClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = (DataGridView)sender;
            if (grid.Columns[e.ColumnIndex].Name == "DeleteGame")
            {
                var id = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["GameId"].Value);
                var title = grid.Rows[e.RowIndex].Cells["Title"].Value?.ToString() ?? "";
                var result = MessageBox.Show($"Delete game '{title}' (ID {id})?\nAll related articles will also be deleted.",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        var ok = await _gameService.DeleteAsync(id);
                        if (ok) await LoadGamesAsync();
                        else MessageBox.Show("Failed to delete game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to delete: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async Task LoadNotificationsAsync()
        {
            try
            {
                flpAdminNotifs.Controls.Clear();
                var allUsers = await _userRepo.GetAllAsync();
                var notifRepo = new NotificationRepository();

                var recentNotifs = new List<Notification>();
                foreach (var user in allUsers)
                {
                    var notifs = await notifRepo.GetByUserIdAsync(user.UserId, false, 5);
                    recentNotifs.AddRange(notifs);
                }

                recentNotifs = recentNotifs.OrderByDescending(n => n.CreatedAt).Take(30).ToList();

                foreach (var n in recentNotifs)
                {
                    var card = ThemeHelper.CreateCardPanel(flpAdminNotifs.Width - 10, 55);
                    var user = allUsers.FirstOrDefault(u => u.UserId == n.UserId);

                    var lblInfo = ThemeHelper.CreateLabel(
                        $"[User #{n.UserId}] {n.Title}", 9.5f, FontStyle.Bold, null, 10, 5);
                    card.Controls.Add(lblInfo);

                    var lblMsg = ThemeHelper.CreateLabel(n.Message, 8.5f, FontStyle.Regular, null, 10, 25);
                    lblMsg.MaximumSize = new Size(card.Width - 120, 40);
                    card.Controls.Add(lblMsg);

                    var lblDate = ThemeHelper.CreateLabel(n.CreatedAt.ToString("MMM dd HH:mm"), 8, FontStyle.Regular, null, card.Width - 110, 5);
                    card.Controls.Add(lblDate);

                    flpAdminNotifs.Controls.Add(card);
                }

                if (!recentNotifs.Any())
                {
                    flpAdminNotifs.Controls.Add(new Label
                    {
                        Text = "No notifications found in the system.",
                        AutoSize = true,
                        Margin = new Padding(12)
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load notifications: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ExportCsvAsync()
        {
            try
            {
                var users = await _userRepo.GetAllAsync();
                using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "users_export.csv" };
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("UserId,Username,Email,Role,CreatedAt");
                foreach (var u in users)
                {
                    var role = u.RoleId == 1 ? "admin" : "user";
                    var line = $"\"{u.UserId}\",\"{Escape(u.Username)}\",\"{Escape(u.Email)}\",\"{role}\",\"{u.CreatedAt:O}\"";
                    sb.AppendLine(line);
                }
                await File.WriteAllTextAsync(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"Exported {users.Count()} users to CSV.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string Escape(string? v) => (v ?? "").Replace("\"", "\"\"");
    }
}