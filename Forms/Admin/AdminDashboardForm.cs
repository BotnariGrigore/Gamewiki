using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using GameWikiApp.Data;
using GameWikiApp.Helpers;

namespace GameWikiApp.Forms.Admin
{
    public class AdminDashboardForm : Form
    {
        public event EventHandler? LogoutRequested;

        private Label lblTotalUsers = new();
        private Label lblTotalAdmins = new();
        private Label lblTotalNormal = new();
        private Label lblTotalWiki = new();
        private TextBox txtSearch = new();
        private DataGridView dgvUsers = new();
        private DataGridView dgvWiki = new();
        private Button btnChangeRole = new();
        private Button btnDeleteUser = new();
        private Button btnDeleteWiki = new();
        private TabControl tabs = new();

        private int _selectedUserId = -1;
        private int _selectedWikiId = -1;

        public AdminDashboardForm()
        {
            Text = "Admin Dashboard";
            BackColor = ThemeHelper.BgPrimary;
            Font = SystemFonts.DefaultFont;
            Padding = new Padding(0, 0, 0, 4);
            BuildUI();
        }

        private void BuildUI()
        {
            Controls.Clear();

            // Top stats bar
            var statsBar = new Panel
            {
                Height = 90,
                Dock = DockStyle.Top,
                BackColor = ThemeHelper.BgSecondary,
                Padding = new Padding(12, 8, 12, 6),
                BorderStyle = BorderStyle.FixedSingle
            };

            var statsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            statsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            statsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));

            var statsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            MakeStatPanel("Total Users:", ref lblTotalUsers, statsFlow);
            MakeStatPanel("Admins:", ref lblTotalAdmins, statsFlow);
            MakeStatPanel("Normal:", ref lblTotalNormal, statsFlow);
            MakeStatPanel("Wiki Pages:", ref lblTotalWiki, statsFlow);
            statsTable.Controls.Add(statsFlow, 0, 0);

            var btnLogout = new Button
            {
                Text = "Logout",
                Size = new Size(100, 28),
                UseVisualStyleBackColor = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (_, __) => LogoutRequested?.Invoke(this, EventArgs.Empty);
            statsTable.Controls.Add(btnLogout, 1, 0);

            statsBar.Controls.Add(statsTable);
            Controls.Add(statsBar);

            // Main content - tabs
            tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(8, 6),
                Margin = new Padding(4, 0, 4, 0)
            };
            tabs.SelectedIndexChanged += (_, __) => LoadDataAsync();

            // Users tab
            var userTab = new TabPage("Users") { Padding = new Padding(6), BackColor = ThemeHelper.BgPrimary };
            var userToolbar = new Panel { Height = 64, Dock = DockStyle.Top, BackColor = ThemeHelper.BgTertiary, Padding = new Padding(6) };
            var userToolbarFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
            txtSearch = new TextBox { PlaceholderText = "Search by username...", Size = new Size(220, 24), BackColor = ThemeHelper.BgInput, ForeColor = ThemeHelper.TextPrimary };
            txtSearch.TextChanged += async (_, __) => await LoadUsersAsync();
            userToolbarFlow.Controls.Add(txtSearch);
            userToolbarFlow.Controls.Add(new Label { Width = 8 });
            btnChangeRole = new Button { Text = "Change Role", Size = new Size(110, 30), BackColor = Color.FromArgb(255, 193, 7), FlatStyle = FlatStyle.Flat, ForeColor = ThemeHelper.TextPrimary };
            btnChangeRole.FlatAppearance.BorderSize = 0;
            btnChangeRole.Click += async (_, __) => await ChangeRoleAsync();
            btnChangeRole.Enabled = false;
            userToolbarFlow.Controls.Add(btnChangeRole);
            btnDeleteUser = new Button { Text = "Delete User", Size = new Size(110, 30), BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDeleteUser.FlatAppearance.BorderSize = 0;
            btnDeleteUser.Click += async (_, __) => await DeleteUserAsync();
            btnDeleteUser.Enabled = false;
            userToolbarFlow.Controls.Add(btnDeleteUser);
            userToolbar.Controls.Add(userToolbarFlow);
            userTab.Controls.Add(userToolbar);

            dgvUsers = BuildStyledGrid();
            dgvUsers.Columns.Clear();
            dgvUsers.Columns.Add("Id", "ID");
            dgvUsers.Columns["Id"].Width = 60;
            dgvUsers.Columns.Add("Username", "Username");
            dgvUsers.Columns.Add("Email", "Email");
            dgvUsers.Columns.Add("Role", "Role");
            dgvUsers.Columns.Add("Created", "Created");
            dgvUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvUsers.SelectionChanged += (_, __) =>
            {
                _selectedUserId = -1;
                if (dgvUsers.SelectedRows.Count > 0 && dgvUsers.SelectedRows[0].Cells["Id"].Value != null)
                    _selectedUserId = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["Id"].Value);
                UpdateActionButtons();
            };
            userTab.Controls.Add(dgvUsers);
            tabs.TabPages.Add(userTab);

            // Wiki tab
            var wikiTab = new TabPage("Wiki Pages") { Padding = new Padding(6), BackColor = ThemeHelper.BgPrimary };
            var wikiToolbar = new Panel { Height = 64, Dock = DockStyle.Top, BackColor = ThemeHelper.BgTertiary, Padding = new Padding(6) };
            var wikiToolbarFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
            btnDeleteWiki = new Button { Text = "Delete Page", Size = new Size(110, 30), BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDeleteWiki.FlatAppearance.BorderSize = 0;
            btnDeleteWiki.Click += async (_, __) => await DeleteWikiAsync();
            btnDeleteWiki.Enabled = false;
            wikiToolbarFlow.Controls.Add(btnDeleteWiki);
            wikiToolbar.Controls.Add(wikiToolbarFlow);
            wikiTab.Controls.Add(wikiToolbar);

            dgvWiki = BuildStyledGrid();
            dgvWiki.Columns.Clear();
            dgvWiki.Columns.Add("Id", "ID");
            dgvWiki.Columns["Id"].Width = 60;
            dgvWiki.Columns.Add("Title", "Title");
            dgvWiki.Columns.Add("Author", "Author");
            dgvWiki.Columns.Add("Created", "Created");
            dgvWiki.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvWiki.SelectionChanged += (_, __) =>
            {
                _selectedWikiId = -1;
                if (dgvWiki.SelectedRows.Count > 0 && dgvWiki.SelectedRows[0].Cells["Id"].Value != null)
                    _selectedWikiId = Convert.ToInt32(dgvWiki.SelectedRows[0].Cells["Id"].Value);
                UpdateActionButtons();
            };
            wikiTab.Controls.Add(dgvWiki);
            tabs.TabPages.Add(wikiTab);

            Controls.Add(tabs);
            LoadDataAsync();
        }

        // ─── Shared styled DataGridView ───
        private DataGridView BuildStyledGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                ReadOnly = true,
                BackColor = ThemeHelper.BgPrimary,
                BackgroundColor = ThemeHelper.BgPrimary,
                ForeColor = ThemeHelper.TextPrimary,
                GridColor = ThemeHelper.Border,
                BorderStyle = BorderStyle.Fixed3D,
                RowTemplate = new DataGridViewRow
                {
                    Height = 30,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = ThemeHelper.BgPrimary,
                        ForeColor = ThemeHelper.TextPrimary,
                        SelectionBackColor = ThemeHelper.Accent,
                        SelectionForeColor = Color.White,
                        Padding = new Padding(4, 0, 0, 0)
                    }
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ThemeHelper.BgSecondary,
                    ForeColor = ThemeHelper.TextPrimary
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ThemeHelper.BgSecondary,
                    ForeColor = ThemeHelper.TextPrimary,
                    Font = new Font(SystemFonts.DefaultFont.FontFamily, 9, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(4, 0, 0, 0)
                },
                ColumnHeadersHeight = 32
            };
        }

        private void UpdateActionButtons()
        {
            btnChangeRole.Enabled = _selectedUserId > 0;
            btnDeleteUser.Enabled = _selectedUserId > 0;
            btnDeleteWiki.Enabled = _selectedWikiId > 0;
        }

        // ─── Stat panel helper ───
        private void MakeStatPanel(string text, ref Label valueLabel, FlowLayoutPanel parent)
        {
            var panel = new Panel
            {
                Width = 150,
                Height = 36,
                Margin = new Padding(0, 0, 12, 4),
                BackColor = ThemeHelper.BgCard,
                Padding = new Padding(8, 2, 4, 2)
            };
            panel.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(4, 2),
                AutoSize = true,
                ForeColor = ThemeHelper.TextMuted,
                Font = new Font(SystemFonts.DefaultFont.FontFamily, 8)
            });
            valueLabel = new Label
            {
                Text = "0",
                Location = new Point(4, 16),
                Font = new Font(SystemFonts.DefaultFont.FontFamily, 11, FontStyle.Bold),
                AutoSize = true,
                ForeColor = ThemeHelper.TextPrimary
            };
            panel.Controls.Add(valueLabel);
            parent.Controls.Add(panel);
        }

        // ─── Data loading ───
        private async void LoadDataAsync()
        {
            try
            {
                using var conn = DbConnection.GetOpen();
                var totalUsers = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");
                var totalAdmins = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE role_id = 1");
                var totalWiki = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM wiki_articles");

                lblTotalUsers.Text = totalUsers.ToString();
                lblTotalAdmins.Text = totalAdmins.ToString();
                lblTotalNormal.Text = (totalUsers - totalAdmins).ToString();
                lblTotalWiki.Text = totalWiki.ToString();

                if (tabs.SelectedIndex == 0) await LoadUsersAsync();
                else await LoadWikiAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dashboard data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                using var conn = DbConnection.GetOpen();
                var q = txtSearch.Text.Trim();
                var sql = "SELECT u.user_id, u.username, u.email, u.created_at, r.role_name FROM users u JOIN roles r ON u.role_id = r.role_id";
                if (!string.IsNullOrEmpty(q))
                    sql += " WHERE u.username LIKE @Q";
                sql += " ORDER BY u.created_at DESC";

                var users = await conn.QueryAsync(sql, new { Q = $"%{q}%" });
                dgvUsers.Rows.Clear();
                _selectedUserId = -1;
                UpdateActionButtons();
                foreach (var u in users)
                {
                    try
                    {
                        int id = Convert.ToInt32(u.user_id);
                        string username = (u.username != null && !(u.username is DBNull)) ? u.username.ToString() : string.Empty;
                        string email = (u.email != null && !(u.email is DBNull)) ? u.email.ToString() : string.Empty;
                        string roleName = (u.role_name != null && !(u.role_name is DBNull)) ? u.role_name.ToString() : string.Empty;
                        string role = roleName.Equals("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "User";
                        string created = string.Empty;
                        if (u.created_at != null && !(u.created_at is DBNull))
                        {
                            if (DateTime.TryParse(u.created_at.ToString(), out DateTime dt))
                                created = dt.ToString("yyyy-MM-dd");
                        }
                        dgvUsers.Rows.Add(id, username, email, role, created);
                    }
                    catch (Exception exRow)
                    {
                        System.Diagnostics.Debug.WriteLine("Skipped user row due to: " + exRow.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading users: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadWikiAsync()
        {
            try
            {
                using var conn = DbConnection.GetOpen();
                var pages = await conn.QueryAsync(@"SELECT a.article_id, a.title, a.created_at, u.username FROM wiki_articles a JOIN users u ON a.author_id = u.user_id ORDER BY a.created_at DESC");
                dgvWiki.Rows.Clear();
                _selectedWikiId = -1;
                UpdateActionButtons();
                foreach (var p in pages)
                {
                    try
                    {
                        int id = Convert.ToInt32(p.article_id);
                        string title = (p.title != null && !(p.title is DBNull)) ? p.title.ToString() : string.Empty;
                        string author = (p.username != null && !(p.username is DBNull)) ? p.username.ToString() : string.Empty;
                        string created = string.Empty;
                        if (p.created_at != null && !(p.created_at is DBNull))
                        {
                            if (DateTime.TryParse(p.created_at.ToString(), out DateTime dt))
                                created = dt.ToString("yyyy-MM-dd");
                        }
                        dgvWiki.Rows.Add(id, title, author, created);
                    }
                    catch (Exception exRow)
                    {
                        System.Diagnostics.Debug.WriteLine("Skipped wiki row due to: " + exRow.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading wiki pages: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Actions ───
        private async Task ChangeRoleAsync()
        {
            if (_selectedUserId <= 0) { MessageBox.Show("Please select a user first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            try
            {
                using var conn = DbConnection.GetOpen();
                var cur = await conn.ExecuteScalarAsync<int>("SELECT role_id FROM users WHERE user_id=@Id", new { Id = _selectedUserId });
                var newRole = cur == 1 ? 2 : 1;
                var name = newRole == 1 ? "Admin" : "User";
                if (MessageBox.Show($"Change selected user to {name}?", "Confirm Role Change", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    await conn.ExecuteAsync("UPDATE users SET role_id=@R WHERE user_id=@Id", new { Id = _selectedUserId, R = newRole });
                    await LoadUsersAsync();
                    LoadDataAsync();
                    lblTotalAdmins.Text = (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE role_id = 1")).ToString();
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async Task DeleteUserAsync()
        {
            if (_selectedUserId <= 0) { MessageBox.Show("Please select a user first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (MessageBox.Show("Delete this user permanently?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using var conn = DbConnection.GetOpen();
                    await conn.ExecuteAsync("DELETE FROM users WHERE user_id=@Id", new { Id = _selectedUserId });
                    _selectedUserId = -1;
                    await LoadUsersAsync();
                    LoadDataAsync();
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        private async Task DeleteWikiAsync()
        {
            if (_selectedWikiId <= 0)
            {
                MessageBox.Show("Please select a wiki page first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Delete this page permanently?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using var conn = DbConnection.GetOpen();
                    await conn.ExecuteAsync("DELETE FROM wiki_articles WHERE article_id=@Id", new { Id = _selectedWikiId });
                    _selectedWikiId = -1;
                    await LoadWikiAsync();
                    LoadDataAsync();
                    UpdateActionButtons();
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

    }

}
