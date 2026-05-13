using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Helpers;
using GameWikiApp.Services;
using GameWikiApp.Models;
using GameWikiApp.Data;
using GameWikiApp.Forms.Admin;
using GameWikiApp.Forms.Auth;
using GameWikiApp.Ui.Controls;

namespace GameWikiApp.Forms.Main
{
    public class HomeForm : Form
    {
        // ── Constants ──
        private const int SIDEBAR_WIDTH = 220;
        private const int TOPBAR_HEIGHT = 70;
        private const int CONTENT_PADDING = 25;
        private const int MENU_ITEM_HEIGHT = 38;
        private const int MENU_ITEM_WIDTH = 196;

        // ── Layout panels ──
        private Panel sidebarPanel;
        private Panel topbarPanel;
        private Panel contentPanel;

        // ── Sidebar ──
        private FlowLayoutPanel menuPanel;
        private Label lblUserInfo;
        private Panel _activeMenuItem;

        // ── Topbar ──
        private TextBox txtSearch;
        private Label lblNotificationBadge;
        private Label lblAvatar;
        private Label btnLogout;

        // ── Pages ──
        private WikiBrowserForm wikiBrowser;
        private ChatForm chatForm;
        private FriendsForm friendsForm;
        private ProfileSettingsForm profileSettings;
        private AdminDashboardForm adminDashboard;

        // ── Services ──
        private readonly ArticleService _articleService = new();
        private readonly GameService _gameService = new();
        private readonly CategoryService _categoryService = new();
        private readonly NotificationService _notificationService = new();

        private string _currentMenuTag = "home";

        public HomeForm()
        {
            Text = "Nexoria - Home";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1400, 850);
            MinimumSize = new Size(1100, 650);

            InitializeContentPanel();
            InitializeSidebar();
            InitializeTopbar();

            ShowHomePage();
            _ = LoadNotificationCount();
            this.FormClosing += async (s, e) =>
            {
                try
                {
                    if (SessionManager.IsAuthenticated)
                    {
                        var user = SessionManager.CurrentUser;
                        if (user != null)
                        {
                            user.IsOnline = false;
                            user.LastSeen = DateTime.UtcNow;
                            await new UserService().UpdateAsync(user);
                        }
                    }
                }
                catch { }
            };
        }

        // ═══════════════════════════════════════════════════
        //  SIDEBAR
        // ═══════════════════════════════════════════════════
        private void InitializeSidebar()
        {
            sidebarPanel = new Panel
            {
                Width = SIDEBAR_WIDTH,
                Dock = DockStyle.Left
            };
            Controls.Add(sidebarPanel);

            // ── Logo ──
            var logoArea = new Panel
            {
                Height = 68,
                Dock = DockStyle.Top,
                Padding = new Padding(18, 18, 18, 0)
            };
            logoArea.Controls.Add(new Label
            {
                Text = "Nexoria",
                AutoSize = true,
                Location = new Point(36, 18)
            });
            logoArea.Controls.Add(new Label
            {
                Text = " ",
                AutoSize = true,
                Location = new Point(4, 16)
            });
            sidebarPanel.Controls.Add(logoArea);

            // ── Spacer + separator ──
            sidebarPanel.Controls.Add(new Panel
            {
                Height = 8,
                Dock = DockStyle.Top
            });

            sidebarPanel.Controls.Add(new Panel
            {
                Height = 1,
                Dock = DockStyle.Top,
                Margin = new Padding(0)
            });

            // ── Menu items ──
            menuPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10, 2, 10, 0)
            };
            sidebarPanel.Controls.Add(menuPanel);

            var menuItems = new (string tag, string icon, string text)[]
            {
                ("home",    "Home", ""),
                ("wiki",    "Wiki", ""),
                ("chat",    "Chat", ""),
                ("friends", "Friends", ""),
                ("profile", "Profile", ""),
                ("notifs",  "Notifications", ""),
            };

            foreach (var item in menuItems)
                menuPanel.Controls.Add(CreateMenuItem(item.tag, item.icon, item.text));

            // ── Admin separator ──
            sidebarPanel.Controls.Add(new Panel
            {
                Height = 1,
                Dock = DockStyle.Bottom
            });

            // ── Admin section ──
            var adminArea = new Panel
            {
                Height = 70,
                Dock = DockStyle.Bottom,
                Padding = new Padding(10, 0, 10, 4)
            };
            var adminBtn = CreateMenuItem("admin", "Admin", "");
            adminBtn.Margin = new Padding(0, 0, 0, 0);
            adminBtn.Width = MENU_ITEM_WIDTH;
            adminBtn.Height = MENU_ITEM_HEIGHT;
            adminArea.Controls.Add(adminBtn);
            sidebarPanel.Controls.Add(adminArea);

            // ── User profile (bottom) ──
            var userArea = new Panel
            {
                Height = 64,
                Dock = DockStyle.Bottom
            };

            lblUserInfo = new Label
            {
                Text = $"{SessionManager.CurrentUser?.Username ?? "User"}",
                Location = new Point(14, 13),
                AutoSize = true
            };
            userArea.Controls.Add(lblUserInfo);

            userArea.Controls.Add(new Label
            {
                Text = $"{SessionManager.CurrentUser?.Email ?? "user@gamewiki.com"}",
                Location = new Point(14, 33),
                AutoSize = true
            });
            sidebarPanel.Controls.Add(userArea);

            // Auto-select Home
            if (menuPanel.Controls.Count > 0 && menuPanel.Controls[0] is Panel firstItem)
                OnMenuItemClick(firstItem, "home");
        }

        private Panel CreateMenuItem(string tag, string icon, string text)
        {
            var container = new Panel
            {
                Height = MENU_ITEM_HEIGHT,
                Width = MENU_ITEM_WIDTH,
                Tag = tag,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 0, 1)
            };

            // Icon (or text)
            var lbl = new Label
            {
                Text = icon,
                Location = new Point(14, 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            container.Controls.Add(lbl);

            // Add click handler
            container.Click += (_, __) => OnMenuItemClick(container, tag);
            lbl.Click += (_, __) => OnMenuItemClick(container, tag);

            return container;
        }

        private void OnMenuItemClick(Panel item, string tag)
        {
            // Deactivate previous
            if (_activeMenuItem != null && _activeMenuItem != item)
            {
                _activeMenuItem.BackColor = Color.Transparent;
            }

            // Activate new
            _activeMenuItem = item;
            item.BackColor = Color.FromArgb(220, 220, 220);

            _currentMenuTag = tag;

            switch (tag)
            {
                case "home":    SwitchToHome(); break;
                case "wiki":    SwitchToWiki(); break;
                case "chat":    SwitchToChat(); break;
                case "friends": SwitchToFriends(); break;
                case "profile": SwitchToProfile(); break;
                case "notifs":  SwitchToNotifications(); break;
                case "admin":   SwitchToAdmin(); break;
            }
        }

        // ═══════════════════════════════════════════════════
        //  TOPBAR
        // ═══════════════════════════════════════════════════
        private void InitializeTopbar()
        {
            topbarPanel = new Panel
            {
                Height = TOPBAR_HEIGHT,
                Dock = DockStyle.Top
            };
            Controls.Add(topbarPanel);

            // Welcome
            var lblWelcome = new Label
            {
                Text = $"Welcome back, {SessionManager.CurrentUser?.Username ?? "User"}!",
                AutoSize = true
            };
            topbarPanel.Controls.Add(lblWelcome);

            // Search
            txtSearch = new TextBox
            {
                Text = "",
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(200, 22)
            };

            var searchBox = new Panel
            {
                Size = new Size(220, 34)
            };
            txtSearch.Location = new Point(8, 6);
            searchBox.Controls.Add(txtSearch);
            topbarPanel.Controls.Add(searchBox);

            // Notification
            var notifBtn = new Label
            {
                Text = "Notify",
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            notifBtn.Click += (_, __) => OnMenuItemClick(_activeMenuItem, "notifs");

            lblNotificationBadge = new Label
            {
                Text = "",
                Size = new Size(16, 16),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            topbarPanel.Controls.Add(lblNotificationBadge);
            topbarPanel.Controls.Add(notifBtn);

            // Logout
            btnLogout = new Label
            {
                Text = "Sign Out",
                Cursor = Cursors.Hand,
                AutoSize = true
            };
            btnLogout.Click += BtnLogout_Click;
            topbarPanel.Controls.Add(btnLogout);

            // ── Layout topbar items on Resize ──
            topbarPanel.Resize += (_, __) => LayoutTopbar(lblWelcome, searchBox, notifBtn, lblNotificationBadge);
            LayoutTopbar(lblWelcome, searchBox, notifBtn, lblNotificationBadge);
        }

        private void LayoutTopbar(Label welcome, Panel search, Label notif, Label badge)
        {
            welcome.Location = new Point(CONTENT_PADDING, (TOPBAR_HEIGHT - welcome.Height) / 2);
            search.Location = new Point(topbarPanel.Width - 380, (TOPBAR_HEIGHT - search.Height) / 2);
            notif.Location = new Point(topbarPanel.Width - 140, (TOPBAR_HEIGHT - notif.Height) / 2);
            badge.Location = new Point(topbarPanel.Width - 145, (TOPBAR_HEIGHT - notif.Height) / 2 - 6);
            btnLogout.Location = new Point(topbarPanel.Width - 100, (TOPBAR_HEIGHT - btnLogout.Height) / 2);
        }

        // ═══════════════════════════════════════════════════
        //  CONTENT PANEL
        // ═══════════════════════════════════════════════════
        private void InitializeContentPanel()
        {
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(CONTENT_PADDING),
                AutoScroll = true
            };
            Controls.Add(contentPanel);
        }

        // ═══════════════════════════════════════════════════
        //  HOME PAGE
        // ═══════════════════════════════════════════════════
        private void ShowHomePage()
        {
            ClearActivePage();

            // Restore padding for home page
            contentPanel.Padding = new Padding(CONTENT_PADDING);

            var page = new Panel { Tag = "home", Dock = DockStyle.Fill };

            // ── Hero ──
            var hero = new Panel
            {
                Height = 120,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 24)
            };
            hero.Controls.Add(new Label
            {
                Text = $"Welcome back, {SessionManager.CurrentUser?.Username ?? "User"}!",
                Location = new Point(28, 24),
                AutoSize = true
            });
            hero.Controls.Add(new Label
            {
                Text = "Explore game communities and wikis",
                Location = new Point(28, 56),
                AutoSize = true
            });

            var btnExplore = new Button
            {
                Text = "Browse Games",
                Size = new Size(130, 32),
                Location = new Point(28, 78),
                UseVisualStyleBackColor = true
            };
            hero.Controls.Add(btnExplore);

            page.Controls.Add(hero);

            // ── Section title ──
            var sectionHeader = new Panel
            {
                Height = 30,
                Dock = DockStyle.Top
            };
            sectionHeader.Controls.Add(new Label
            {
                Text = "Popular Games",
                AutoSize = true,
                Location = new Point(0, 4)
            });
            page.Controls.Add(sectionHeader);

            // ── Game grid ──
            var grid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false,
                Padding = new Padding(0, 8, 0, 0)
            };
            page.Controls.Add(grid);

            contentPanel.Controls.Add(page);
            _ = LoadGamesAsync(grid);
        }

        private async Task LoadGamesAsync(FlowLayoutPanel grid)
        {
            try
            {
                var games = (await _gameService.GetAllAsync())?.ToList() ?? new List<Game>();

                if (games.Count == 0)
                {
                    grid.Controls.Add(new Label
                    {
                        Text = "  No games available yet.",
                        AutoSize = true,
                        Padding = new Padding(15)
                    });
                    return;
                }

                foreach (var game in games)
                {
                    var card = new GameCard
                    {
                        GameName = game.Title,
                        Genre = game.ShortDescription ?? "No description",
                        Rating = "4.5"
                    };

                    if (!string.IsNullOrEmpty(game.CoverImage))
                    {
                        try
                        {
                            var coverPath = game.CoverImage.Replace('/', Path.DirectorySeparatorChar);
                            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, coverPath);
                            if (File.Exists(local))
                                card.GameImage = Image.FromFile(local);
                            else
                            {
                                using var client = new System.Net.WebClient();
                                var data = client.DownloadData(game.CoverImage);
                                using var ms = new MemoryStream(data);
                                card.GameImage = new Bitmap(ms);
                            }
                        }
                        catch { card.SetPlaceholderImage(Color.FromArgb(200, 200, 200), " "); }
                    }
                    else
                    {
                        card.SetPlaceholderImage(Color.FromArgb(200, 200, 200), " ");
                    }

                    grid.Controls.Add(card);
                }
            }
            catch (Exception ex)
            {
                grid.Controls.Add(new Label
                {
                    Text = $"  Failed to load games: {ex.Message}",
                    AutoSize = true,
                    Padding = new Padding(15)
                });
            }
        }

        // ═══════════════════════════════════════════════════
        //  NAVIGATION
        // ═══════════════════════════════════════════════════
        private void SwitchToHome()
        {
            ClearActivePage();
            ShowHomePage();
        }

        private void SwitchToWiki()
        {
            ClearActivePage();
            wikiBrowser = new WikiBrowserForm { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Tag = "wiki", Dock = DockStyle.Fill };
            contentPanel.Controls.Add(wikiBrowser);
            wikiBrowser.Show();
        }

        private void SwitchToChat()
        {
            ClearActivePage();
            chatForm = new ChatForm { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Tag = "chat", Dock = DockStyle.Fill };
            contentPanel.Controls.Add(chatForm);
            chatForm.Show();
        }

        private void SwitchToFriends()
        {
            ClearActivePage();
            friendsForm = new FriendsForm { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Tag = "friends", Dock = DockStyle.Fill };
            contentPanel.Controls.Add(friendsForm);
            friendsForm.Show();
        }

        private void SwitchToProfile()
        {
            ClearActivePage();
            profileSettings = new ProfileSettingsForm { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Tag = "profile", Dock = DockStyle.Fill };
            contentPanel.Controls.Add(profileSettings);
            profileSettings.Show();
        }

        private void SwitchToAdmin()
        {
            ClearActivePage();

            // Remove padding from contentPanel so the dashboard fills the area
            contentPanel.Padding = new Padding(0);

            // Dispose old instance
            if (adminDashboard != null)
            {
                try { adminDashboard.Dispose(); } catch { }
                adminDashboard = null;
            }

            adminDashboard = new AdminDashboardForm();
            adminDashboard.LogoutRequested += (_, __) =>
            {
                contentPanel.Controls.Clear();
                if (adminDashboard != null)
                {
                    try { adminDashboard.Dispose(); } catch { }
                    adminDashboard = null;
                }
                SessionManager.EndSession();
                Close();
                new Auth.LoginForm().Show();
            };
            adminDashboard.TopLevel = false;
            adminDashboard.FormBorderStyle = FormBorderStyle.None;
            adminDashboard.Tag = "admin";
            adminDashboard.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(adminDashboard);
            adminDashboard.Show();
        }

        private void SwitchToNotifications()
        {
            ClearActivePage();
            var panel = new Panel { Tag = "notifications", Dock = DockStyle.Fill };
            panel.Controls.Add(new Label
            {
                Text = "Notifications",
                AutoSize = true
            });

            var flp = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
            panel.Controls.Add(flp);
            contentPanel.Controls.Add(panel);
            _ = LoadNotificationsUI(flp);
        }

        private async Task LoadNotificationsUI(FlowLayoutPanel flp)
        {
            try
            {
                var notifs = await _notificationService.GetByUserIdAsync(SessionManager.CurrentUser!.UserId, limit: 50);
                var list = notifs?.ToList() ?? new List<Data.Notification>();

                if (list.Count == 0)
                {
                    flp.Controls.Add(new Label { Text = "  No notifications.", AutoSize = true, Padding = new Padding(12) });
                    return;
                }

                foreach (var n in list)
                {
                    var card = ThemeHelper.CreateCardPanel(flp.Width - 10, 55);
                    card.Controls.Add(new Label { Text = n.Title, Location = new Point(12, 6), AutoSize = true });
                    card.Controls.Add(new Label { Text = n.Message, Location = new Point(12, 26), AutoSize = true, MaximumSize = new Size(card.Width - 100, 40) });

                    if (!n.IsRead)
                    {
                        var btn = new Button
                        {
                            Text = "Read",
                            Size = new Size(65, 26),
                            Location = new Point(card.Width - 85, 14),
                            UseVisualStyleBackColor = true
                        };
                        btn.Click += async (_, __) =>
                        {
                            await _notificationService.MarkAsReadAsync(n.NotificationId);
                            _ = LoadNotificationsUI(flp);
                            await LoadNotificationCount();
                        };
                        card.Controls.Add(btn);
                    }
                    flp.Controls.Add(card);
                }
            }
            catch { }
        }

        private void ClearActivePage()
        {
            for (int i = contentPanel.Controls.Count - 1; i >= 0; i--)
            {
                var ctrl = contentPanel.Controls[i];
                if (ctrl is Form f) { f.Close(); f.Dispose(); }
                if (ctrl is Panel p && p.Tag != null) p.Dispose();
            }
        }

        // ═══════════════════════════════════════════════════
        //  EVENTS
        // ═══════════════════════════════════════════════════
        private async void BtnLogout_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to sign out?", "Sign Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    if (SessionManager.IsAuthenticated)
                    {
                        var user = SessionManager.CurrentUser;
                        if (user != null)
                        {
                            user.IsOnline = false;
                            user.LastSeen = DateTime.UtcNow;
                            await new UserService().UpdateAsync(user);
                        }
                    }
                }
                catch { }

                SessionManager.EndSession();
                Close();
                new LoginForm().Show();
            }
        }

        private async Task LoadNotificationCount()
        {
            try
            {
                if (SessionManager.IsAuthenticated)
                {
                    var count = await _notificationService.GetUnreadCountAsync(SessionManager.CurrentUser!.UserId);
                    lblNotificationBadge.Text = count > 0 ? count.ToString() : "";
                    lblNotificationBadge.Visible = count > 0;
                }
            }
            catch { }
        }
    }
}