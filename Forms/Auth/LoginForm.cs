using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Services;

namespace GameWikiApp.Forms.Auth
{
    public class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblError;

        private readonly AuthService _auth = new();

        // Colors
        private readonly Color BackColorDark = Color.FromArgb(18, 18, 30);
        private readonly Color CardColor = Color.FromArgb(28, 28, 48);
        private readonly Color AccentColor = Color.FromArgb(0, 180, 216);
        private readonly Color AccentHover = Color.FromArgb(0, 150, 200);
        private readonly Color TextColor = Color.FromArgb(220, 220, 240);
        private readonly Color MutedText = Color.FromArgb(140, 140, 170);
        private readonly Color InputBg = Color.FromArgb(38, 38, 60);
        private readonly Color BorderColor = Color.FromArgb(60, 60, 90);

        public LoginForm()
        {
            InitializeForm();
            InitializeControls();
        }

        private void InitializeForm()
        {
            Text = "GameWiki - Sign In";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(440, 520);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = BackColorDark;
            ForeColor = TextColor;
            Font = new Font("Segoe UI", 10);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

        private void InitializeControls()
        {
            // ── Card panel ──
            var card = new Panel
            {
                Size = new Size(380, 440),
                Location = new Point((ClientSize.Width - 380) / 2, (ClientSize.Height - 440) / 2),
                BackColor = CardColor,
            };
            card.Paint += (s, e) =>
            {
                // Rounded corners
                using var path = GetRoundedPath(card.ClientRectangle, 12);
                using var brush = new SolidBrush(CardColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
                // Border
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawPath(pen, path);
            };
            Controls.Add(card);

            // ── Logo / Title ──
            var lblIcon = new Label
            {
                Text = "🎮",
                Font = new Font("Segoe UI", 36),
                ForeColor = AccentColor,
                AutoSize = true,
                Location = new Point(170, 20)
            };
            card.Controls.Add(lblIcon);

            var lblTitle = new Label
            {
                Text = "Welcome Back",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point((380 - TextRenderer.MeasureText("Welcome Back", new Font("Segoe UI", 20, FontStyle.Bold)).Width) / 2, 72)
            };
            card.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Sign in to continue to GameWiki",
                Font = new Font("Segoe UI", 10),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point((380 - TextRenderer.MeasureText("Sign in to continue to GameWiki", new Font("Segoe UI", 10)).Width) / 2, 104)
            };
            card.Controls.Add(lblSubtitle);

            // ── Username ──
            var lblUser = new Label
            {
                Text = "  👤  Username",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(30, 148)
            };
            card.Controls.Add(lblUser);

            txtUsername = new TextBox
            {
                Location = new Point(30, 172),
                Size = new Size(320, 36),
                BackColor = InputBg,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                Text = ""
            };
            txtUsername.Enter += (_, __) => txtUsername.BackColor = Color.FromArgb(48, 48, 75);
            txtUsername.Leave += (_, __) => txtUsername.BackColor = InputBg;
            WrapTextBox(card, txtUsername);

            // ── Password ──
            var lblPass = new Label
            {
                Text = "  🔒  Password",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(30, 228)
            };
            card.Controls.Add(lblPass);

            txtPassword = new TextBox
            {
                Location = new Point(30, 252),
                Size = new Size(320, 36),
                BackColor = InputBg,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true
            };
            txtPassword.Enter += (_, __) => txtPassword.BackColor = Color.FromArgb(48, 48, 75);
            txtPassword.Leave += (_, __) => txtPassword.BackColor = InputBg;
            WrapTextBox(card, txtPassword);

            // ── Error label ──
            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(255, 100, 100),
                AutoSize = true,
                Location = new Point(30, 306),
                Visible = false
            };
            card.Controls.Add(lblError);

            // ── Login button ──
            btnLogin = new Button
            {
                Text = "Sign In",
                Location = new Point(30, 320),
                Size = new Size(320, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Paint += (s, e) =>
            {
                var btn = (Button)s;
                using var path = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), 8);
                using var brush = new LinearGradientBrush(btn.ClientRectangle, AccentColor, Color.FromArgb(72, 0, 180), LinearGradientMode.Horizontal);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnLogin.MouseEnter += (_, __) => { btnLogin.BackColor = AccentHover; btnLogin.Invalidate(); };
            btnLogin.MouseLeave += (_, __) => { btnLogin.BackColor = AccentColor; btnLogin.Invalidate(); };
            btnLogin.Click += async (_, __) => await OnLogin();
            card.Controls.Add(btnLogin);

            // ── Divider ──
            var lblOr = new Label
            {
                Text = "──────────  or  ──────────",
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point((380 - TextRenderer.MeasureText("──────────  or  ──────────", new Font("Segoe UI", 9)).Width) / 2, 376)
            };
            card.Controls.Add(lblOr);

            // ── Register link ──
            var lblRegister = new Label
            {
                Text = "Don't have an account?  Create one",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point((380 - TextRenderer.MeasureText("Don't have an account?  Create one", new Font("Segoe UI", 9.5f)).Width) / 2, 400),
                Cursor = Cursors.Hand
            };
            lblRegister.MouseEnter += (_, __) => lblRegister.ForeColor = AccentColor;
            lblRegister.MouseLeave += (_, __) => lblRegister.ForeColor = MutedText;
            lblRegister.Click += (_, __) => OpenRegister();
            card.Controls.Add(lblRegister);

            AcceptButton = btnLogin;
        }

        private void WrapTextBox(Control parent, TextBox tb)
        {
            var wrapper = new Panel
            {
                Location = tb.Location,
                Size = tb.Size,
                BackColor = tb.BackColor
            };
            wrapper.Paint += (s, e) =>
            {
                using var path = GetRoundedPath(new Rectangle(0, 0, wrapper.Width, wrapper.Height), 8);
                using var brush = new SolidBrush(wrapper.BackColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawPath(pen, path);
            };
            tb.Location = new Point(12, 8);
            tb.Size = new Size(wrapper.Width - 24, wrapper.Height - 16);
            tb.BackColor = wrapper.BackColor;
            parent.Controls.Add(wrapper);
            parent.Controls.Remove(tb);
            wrapper.Controls.Add(tb);
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
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

            var user = await _auth.AuthenticateAsync(username, password);
            btnLogin.Enabled = true;

            if (user != null)
            {
                Helpers.SessionManager.StartSession(user, Guid.NewGuid().ToString());
                Hide();
                var home = new Main.HomeForm(user.Username);
                home.FormClosed += (_, __) => Close();
                home.Show();
            }
            else
            {
                ShowError("Invalid username or password.");
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = "  ⚠  " + message;
            lblError.Visible = true;
        }

        private void OpenRegister()
        {
            using var reg = new RegisterForm();
            reg.ShowDialog(this);
        }
    }
}