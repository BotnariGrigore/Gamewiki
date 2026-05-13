using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Services;
using GameWikiApp.Helpers;
using GameWikiApp.Forms.Main;

namespace GameWikiApp.Forms.Auth
{
    public class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblError;

        private readonly AuthService _auth = new();

        public LoginForm()
        {
            InitializeForm();
            InitializeControls();
        }

        private void InitializeForm()
        {
            Text = "Nexoria - Sign In";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(460, 560);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = ThemeHelper.BgPrimary;
            ForeColor = ThemeHelper.TextPrimary;
            Font = new Font("Segoe UI", 10);
        }

        private void InitializeControls()
        {
            var card = new Panel
            {
                Size = new Size(400, 480),
                Location = new Point((ClientSize.Width - 400) / 2, (ClientSize.Height - 480) / 2),
                BackColor = ThemeHelper.BgSecondary,
            };
            card.Paint += (s, e) =>
            {
                using var path = ThemeHelper.GetRoundedPath(card.ClientRectangle, ThemeHelper.BorderRadiusLarge);
                using var brush = new SolidBrush(card.BackColor);
                using var pen = new Pen(ThemeHelper.BorderLight, 1);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            };
            Controls.Add(card);

            // Logo
            var lblIcon = new Label
            {
                Text = "🎮",
                Font = new Font("Segoe UI", 40),
                ForeColor = ThemeHelper.Accent,
                AutoSize = true,
                Location = new Point((card.Width - 60) / 2, 20)
            };
            card.Controls.Add(lblIcon);

            var lblTitle = ThemeHelper.CreateLabel("Welcome Back", 22, FontStyle.Bold, ThemeHelper.TextPrimary,
                (card.Width - TextRenderer.MeasureText("Welcome Back", new Font("Segoe UI", 22, FontStyle.Bold)).Width) / 2, 78);
            card.Controls.Add(lblTitle);

            var lblSubtitle = ThemeHelper.CreateLabel("Sign in to access your account", 10, FontStyle.Regular, ThemeHelper.TextSecondary,
                (card.Width - TextRenderer.MeasureText("Sign in to access your account", new Font("Segoe UI", 10)).Width) / 2, 110);
            card.Controls.Add(lblSubtitle);

            // Username
            var lblUser = ThemeHelper.CreateLabel("👤  Username or Email", 10, FontStyle.Regular, ThemeHelper.TextSecondary, ThemeHelper.SpacingMedium, 155);
            card.Controls.Add(lblUser);

            txtUsername = new TextBox
            {
                Location = new Point(ThemeHelper.SpacingMedium, 180),
                Size = new Size(340, 40),
                BackColor = ThemeHelper.BgInput,
                ForeColor = ThemeHelper.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Enter username or email"
            };
            txtUsername.Enter += (_, __) => txtUsername.BackColor = ThemeHelper.BgTertiary;
            txtUsername.Leave += (_, __) => txtUsername.BackColor = ThemeHelper.BgInput;
            var wrap1 = ThemeHelper.WrapInput(txtUsername, 346, 44);
            wrap1.Location = new Point(27, 178);
            card.Controls.Add(wrap1);

            // Password
            var lblPass = ThemeHelper.CreateLabel("🔒  Password", 10, FontStyle.Regular, ThemeHelper.TextSecondary, ThemeHelper.SpacingMedium, 238);
            card.Controls.Add(lblPass);

            txtPassword = new TextBox
            {
                Location = new Point(ThemeHelper.SpacingMedium, 263),
                Size = new Size(340, 40),
                BackColor = ThemeHelper.BgInput,
                ForeColor = ThemeHelper.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true,
                PlaceholderText = "Enter password"
            };
            txtPassword.Enter += (_, __) => txtPassword.BackColor = ThemeHelper.BgTertiary;
            txtPassword.Leave += (_, __) => txtPassword.BackColor = ThemeHelper.BgInput;
            var wrap2 = ThemeHelper.WrapInput(txtPassword, 346, 44);
            wrap2.Location = new Point(27, 261);
            card.Controls.Add(wrap2);

            // Error
            lblError = new Label
            {
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeHelper.Error,
                AutoSize = true,
                Location = new Point(ThemeHelper.SpacingMedium, 312),
                Visible = false
            };
            card.Controls.Add(lblError);

            // Login button
            btnLogin = ThemeHelper.CreateThemedButton("Sign In", ThemeHelper.SpacingMedium, 328, 340, 44);
            btnLogin.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += async (_, __) => await OnLogin();
            card.Controls.Add(btnLogin);

            // Divider
            var divider = ThemeHelper.CreateSeparator(280, 0, 386);
            divider.Location = new Point((card.Width - 280) / 2, 384);
            card.Controls.Add(divider);

            var lblOr = ThemeHelper.CreateLabel("or", 10, FontStyle.Bold, ThemeHelper.TextMuted,
                (card.Width - TextRenderer.MeasureText("or", new Font("Segoe UI", 10, FontStyle.Bold)).Width) / 2, 394);
            card.Controls.Add(lblOr);

            // Register link
            var lblRegister = ThemeHelper.CreateLabel("Don't have an account?  Create one", 10, FontStyle.Regular, ThemeHelper.TextMuted,
                (card.Width - TextRenderer.MeasureText("Don't have an account?  Create one", new Font("Segoe UI", 10)).Width) / 2, 418);
            lblRegister.Cursor = Cursors.Hand;
            lblRegister.MouseEnter += (_, __) => lblRegister.ForeColor = ThemeHelper.Accent;
            lblRegister.MouseLeave += (_, __) => lblRegister.ForeColor = ThemeHelper.TextMuted;
            lblRegister.Click += (_, __) => { using var reg = new RegisterForm(); reg.ShowDialog(this); };
            card.Controls.Add(lblRegister);

            AcceptButton = btnLogin;
        }

        private async Task OnLogin()
        {
            btnLogin.Enabled = false;
            lblError.Visible = false;

            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter username and password.");
                btnLogin.Enabled = true;
                return;
            }

            var (user, error) = await _auth.AuthenticateAsync(username, password);
            btnLogin.Enabled = true;

            if (error != null)
            {
                ShowError(error);
                return;
            }

            if (user != null)
            {
                try
                {
                    Helpers.SessionManager.StartSession(user, Guid.NewGuid().ToString());
                    Hide();
                    var home = new HomeForm();
                    home.FormClosed += (_, __) => { Environment.Exit(0); };
                    home.Show();
                }
                catch (Exception ex)
                {
                    try { System.IO.File.AppendAllText("auth_errors.log", DateTime.UtcNow + " LOGIN SESSION ERROR: " + ex + Environment.NewLine); } catch {}
                    ShowError("Failed to start session. Please try again.");
                }
            }
            else
            {
                ShowError("Invalid username/email or password.");
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = "  ⚠  " + message;
            lblError.Visible = true;
        }
    }
}