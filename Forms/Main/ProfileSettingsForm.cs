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
using GameWikiApp.Forms.Auth;

namespace GameWikiApp.Forms.Main
{
    public class ProfileSettingsForm : Form
    {
        private TabControl tabControl;
        private TextBox txtUsername;
        private TextBox txtEmail;
        private TextBox txtBio;
        private ComboBox cmbTheme;
        private Button btnSaveProfile;
        private Label lblUsername;
        private Label lblEmail;
        private Label lblBio;
        private Label lblThemeTitle;
        private Label lblStatus;
        private PictureBox pbAvatar;
        private Label lblNotificationCount;

        private readonly UserService _userService = new();
        private readonly NotificationService _notificationService = new();

        public ProfileSettingsForm()
        {
            Text = "Profile & Settings";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(800, 600);
            MinimumSize = new Size(600, 450);

            InitializeLayout();
            LoadUserData();
        }

        private void InitializeLayout()
        {
            // Header
            var header = new Panel
            {
                Height = 70,
                Dock = DockStyle.Top,
                Padding = new Padding(20, 12, 20, 12)
            };
            Controls.Add(header);

            var titleLabel = ThemeHelper.CreateLabel("Profile & Settings", 20, FontStyle.Bold, null, 0, 12);
            header.Controls.Add(titleLabel);

            // Tab control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // -- Profile tab --
            var tabProfile = new TabPage("Profile");

            var pnlProfile = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24)
            };
            tabProfile.Controls.Add(pnlProfile);

            // Avatar area
            pbAvatar = new PictureBox
            {
                Size = new Size(80, 80),
                Location = new Point(0, 0),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlProfile.Controls.Add(pbAvatar);

            lblUsername = ThemeHelper.CreateLabel("", 16, FontStyle.Bold, null, 95, 5);
            pnlProfile.Controls.Add(lblUsername);

            lblEmail = ThemeHelper.CreateLabel("", 10, FontStyle.Regular, null, 95, 28);
            pnlProfile.Controls.Add(lblEmail);

            // Separator
            var sep1 = ThemeHelper.CreateSeparator(pnlProfile.Width - 48, 0, 95);
            sep1.Location = new Point(24, 95);
            pnlProfile.Controls.Add(sep1);

            // Username field
            pnlProfile.Controls.Add(ThemeHelper.CreateLabel("Username:", 10, FontStyle.Bold, null, 0, 110));

            txtUsername = new TextBox
            {
                PlaceholderText = "Username",
                Size = new Size(400, 36)
            };
            var wrapUser = ThemeHelper.WrapInput(txtUsername, 406, 40);
            wrapUser.Location = new Point(0, 132);
            pnlProfile.Controls.Add(wrapUser);

            // Email field
            pnlProfile.Controls.Add(ThemeHelper.CreateLabel("Email:", 10, FontStyle.Bold, null, 0, 182));

            txtEmail = new TextBox
            {
                PlaceholderText = "Email",
                Size = new Size(400, 36)
            };
            var wrapEmail = ThemeHelper.WrapInput(txtEmail, 406, 40);
            wrapEmail.Location = new Point(0, 204);
            pnlProfile.Controls.Add(wrapEmail);

            // Bio field
            pnlProfile.Controls.Add(ThemeHelper.CreateLabel("Bio:", 10, FontStyle.Bold, null, 0, 255));

            txtBio = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Size = new Size(400, 100),
                MaxLength = 500
            };
            var wrapBio = ThemeHelper.WrapInput(txtBio, 406, 104);
            wrapBio.Location = new Point(0, 277);
            pnlProfile.Controls.Add(wrapBio);

            // Save button
            btnSaveProfile = ThemeHelper.CreateThemedButton("Save Profile", 0, 395, 160, 38);
            btnSaveProfile.Click += async (_, __) => await SaveProfileAsync();
            pnlProfile.Controls.Add(btnSaveProfile);

            lblStatus = ThemeHelper.CreateLabel("", 9.5f, FontStyle.Regular, null, 0, 445);
            pnlProfile.Controls.Add(lblStatus);

            // -- Notifications tab --
            var tabNotifications = new TabPage("Notifications");

            var pnlNotifications = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24)
            };
            tabNotifications.Controls.Add(pnlNotifications);

            var headerNotif = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top
            };
            pnlNotifications.Controls.Add(headerNotif);

            var lblNotifTitle = ThemeHelper.CreateLabel("Your Notifications", 14, FontStyle.Bold, null, 0, 10);
            headerNotif.Controls.Add(lblNotifTitle);

            lblNotificationCount = ThemeHelper.CreateLabel("", 9, FontStyle.Regular, null, 200, 12);
            headerNotif.Controls.Add(lblNotificationCount);

            var btnMarkAllRead = ThemeHelper.CreateThemedButton("Mark All Read", 300, 8, 120, 26);
            btnMarkAllRead.Click += async (_, __) =>
            {
                await _notificationService.MarkAllAsReadAsync(SessionManager.CurrentUser!.UserId);
                _ = RefreshNotifications();
            };
            headerNotif.Controls.Add(btnMarkAllRead);

            var flpNotifications = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            flpNotifications.Name = "flpNotifications";
            pnlNotifications.Controls.Add(flpNotifications);

            // -- Settings tab --
            var tabSettings = new TabPage("Settings");

            var pnlSettings = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24)
            };
            tabSettings.Controls.Add(pnlSettings);

            pnlSettings.Controls.Add(ThemeHelper.CreateLabel("Account", 16, FontStyle.Bold, null, 0, 10));
            pnlSettings.Controls.Add(ThemeHelper.CreateLabel("Manage your account", 9.5f, FontStyle.Regular, null, 0, 34));

            var btnLogout = ThemeHelper.CreateThemedButton("Sign Out", 0, 230, 140, 36);
            btnLogout.Click += (_, __) =>
            {
                SessionManager.EndSession();
                Close();
                var login = new LoginForm();
                login.Show();
            };
            pnlSettings.Controls.Add(btnLogout);

            var btnDeleteAccount = ThemeHelper.CreateThemedButton("Delete Account", 160, 230, 160, 36);
            btnDeleteAccount.Click += async (_, __) =>
            {
                var confirm = MessageBox.Show(
                    "Are you sure you want to delete your account? This action is irreversible and will delete all your data.",
                    "Delete Account",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        var repo = new UserRepository();
                        var ok = await repo.DeleteAsync(SessionManager.CurrentUser!.UserId);
                        if (ok)
                        {
                            SessionManager.EndSession();
                            MessageBox.Show("Account deleted.", "Farewell", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Close();
                            var login = new LoginForm();
                            login.Show();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to delete: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            pnlSettings.Controls.Add(btnDeleteAccount);

            // Add tabs
            tabControl.TabPages.Add(tabProfile);
            tabControl.TabPages.Add(tabNotifications);
            tabControl.TabPages.Add(tabSettings);

            Controls.Add(tabControl);
        }

        private void LoadUserData()
        {
            var user = SessionManager.CurrentUser;
            if (user == null) return;

            lblUsername.Text = "@" + user.Username;
            lblEmail.Text = user.Email;
            txtUsername.Text = user.Username;
            txtEmail.Text = user.Email;
            txtBio.Text = user.Bio ?? "";

            if (!string.IsNullOrEmpty(user.ProfileImage))
            {
                try
                {
                    var path = user.ProfileImage.Replace('/', Path.DirectorySeparatorChar);
                    var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                    if (File.Exists(local))
                        pbAvatar.Image = Image.FromFile(local);
                    else
                        pbAvatar.Image = null;
                }
                catch
                {
                    pbAvatar.Image = null;
                }
            }
            else
            {
                pbAvatar.Image = null;
            }
        }

        private async Task SaveProfileAsync()
        {
            try
            {
                var user = SessionManager.CurrentUser!;
                user.Username = txtUsername.Text.Trim();
                user.Email = txtEmail.Text.Trim();
                user.Bio = txtBio.Text.Trim();

                var repo = new UserRepository();
                var ok = await repo.UpdateAsync(user);

                if (ok)
                {
                    lblStatus.Text = "Profile saved successfully";
                    lblStatus.Visible = true;

                    lblUsername.Text = "@" + user.Username;
                    lblEmail.Text = user.Email;
                }
                else
                {
                    lblStatus.Text = "Failed to save profile";
                    lblStatus.Visible = true;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
                lblStatus.Visible = true;
            }
        }

        private async Task RefreshNotifications()
        {
            var userId = SessionManager.CurrentUser!.UserId;
            var notifications = await _notificationService.GetByUserIdAsync(userId, limit: 50);

            var flp = Controls.Find("flpNotifications", true).FirstOrDefault() as FlowLayoutPanel;
            if (flp == null) return;

            flp.Controls.Clear();

            if (!notifications.Any())
            {
                flp.Controls.Add(new Label
                {
                    Text = "No notifications yet.",
                    AutoSize = true,
                    Margin = new Padding(12)
                });
            }

            foreach (var n in notifications)
            {
                var card = ThemeHelper.CreateCardPanel(flp.Width - 20, 60);

                var lblTitle = ThemeHelper.CreateLabel(n.Title, 10, FontStyle.Bold, null, 12, 8);
                card.Controls.Add(lblTitle);

                var lblMsg = ThemeHelper.CreateLabel(n.Message, 9, FontStyle.Regular, null, 12, 26);
                lblMsg.MaximumSize = new Size(card.Width - 80, 40);
                card.Controls.Add(lblMsg);

                if (!n.IsRead)
                {
                    var btnRead = ThemeHelper.CreateThemedButton("Mark Read", card.Width - 110, 16, 80, 28);
                    btnRead.Click += async (_, __) =>
                    {
                        await _notificationService.MarkAsReadAsync(n.NotificationId);
                        _ = RefreshNotifications();
                        var count = await _notificationService.GetUnreadCountAsync(userId);
                        lblNotificationCount.Text = count > 0 ? $"{count} unread" : "All read";
                    };
                    card.Controls.Add(btnRead);
                }

                flp.Controls.Add(card);
            }

            var count = await _notificationService.GetUnreadCountAsync(userId);
            lblNotificationCount.Text = count > 0 ? $"{count} unread" : "All read";
        }
    }
}