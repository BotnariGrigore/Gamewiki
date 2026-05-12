using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Services;

namespace GameWikiApp.Forms.Auth
{
    public class RegisterForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtEmail;
        private TextBox txtPassword;
        private TextBox txtConfirm;
        private Button btnRegister;
        private Label lblError;

        private readonly AuthService _auth = new();

        // Colors matching LoginForm
        private readonly Color BackColorDark = Color.FromArgb(18, 18, 30);
        private readonly Color CardColor = Color.FromArgb(28, 28, 48);
        private readonly Color AccentColor = Color.FromArgb(0, 180, 216);
        private readonly Color AccentHover = Color.FromArgb(0, 150, 200);
        private readonly Color TextColor = Color.FromArgb(220, 220, 240);
        private readonly Color MutedText = Color.FromArgb(140, 140, 170);
        private readonly Color InputBg = Color.FromArgb(38, 38, 60);
        private readonly Color BorderColor = Color.FromArgb(60, 60, 90);

        public RegisterForm()
        {
            InitializeForm();
            InitializeControls();
        }

        private void InitializeForm()
        {
            Text = "GameWiki - Create Account";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(440, 600);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = BackColorDark;
            ForeColor = TextColor;
            Font = new Font("Segoe UI", 10);
        }

        private void InitializeControls()
        {
            // ── Card panel ──
            var card = new Panel
            {
                Size = new Size(380, 520),
                Location = new Point((ClientSize.Width - 380) / 2, (ClientSize.Height - 520) / 2),
                BackColor = CardColor,
            };
            card.Paint += (s, e) =>
            {
                using var path = GetRoundedPath(card.ClientRectangle, 12);
                using var brush = new SolidBrush(CardColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawPath(pen, path);
            };
            Controls.Add(card);

            // ── Logo / Title ──
            var lblIcon = new Label
            {
                Text = "🚀",
                Font = new Font("Segoe UI", 36),
                ForeColor = AccentColor,
                AutoSize = true,
                Location = new Point(170, 16)
            };
            card.Controls.Add(lblIcon);

            var lblTitle = new Label
            {
                Text = "Create Account",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point((380 - TextRenderer.MeasureText("Create Account", new Font("Segoe UI", 20, FontStyle.Bold)).Width) / 2, 68)
            };
            card.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Join the GameWiki community",
                Font = new Font("Segoe UI", 10),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point((380 - TextRenderer.MeasureText("Join the GameWiki community", new Font("Segoe UI", 10)).Width) / 2, 100)
            };
            card.Controls.Add(lblSubtitle);

            // ── Username ──
            var lblUser = new Label
            {
                Text = "  👤  Username",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(30, 136)
            };
            card.Controls.Add(lblUser);

            txtUsername = new TextBox
            {
                Location = new Point(30, 160),
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

            // ── Email ──
            var lblEmail = new Label
            {
                Text = "  📧  Email",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(30, 208)
            };
            card.Controls.Add(lblEmail);

            txtEmail = new TextBox
            {
                Location = new Point(30, 232),
                Size = new Size(320, 36),
                BackColor = InputBg,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11)
            };
            txtEmail.Enter += (_, __) => txtEmail.BackColor = Color.FromArgb(48, 48, 75);
            txtEmail.Leave += (_, __) => txtEmail.BackColor = InputBg;
            WrapTextBox(card, txtEmail);

            // ── Password ──
            var lblPass = new Label
            {
                Text = "  🔒  Password",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(30, 280)
            };
            card.Controls.Add(lblPass);

            txtPassword = new TextBox
            {
                Location = new Point(30, 304),
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

            // ── Confirm ──
            var lblConfirm = new Label
            {
                Text = "  ✅  Confirm Password",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(30, 352)
            };
            card.Controls.Add(lblConfirm);

            txtConfirm = new TextBox
            {
                Location = new Point(30, 376),
                Size = new Size(320, 36),
                BackColor = InputBg,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true
            };
            txtConfirm.Enter += (_, __) => txtConfirm.BackColor = Color.FromArgb(48, 48, 75);
            txtConfirm.Leave += (_, __) => txtConfirm.BackColor = InputBg;
            WrapTextBox(card, txtConfirm);

            // ── Error label ──
            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(255, 100, 100),
                AutoSize = true,
                Location = new Point(30, 424),
                Visible = false
            };
            card.Controls.Add(lblError);

            // ── Register button ──
            btnRegister = new Button
            {
                Text = "Create Account",
                Location = new Point(30, 442),
                Size = new Size(320, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Paint += (s, e) =>
            {
                var btn = (Button)s;
                using var path = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), 8);
                using var brush = new LinearGradientBrush(btn.ClientRectangle, Color.FromArgb(72, 0, 180), AccentColor, LinearGradientMode.Horizontal);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnRegister.MouseEnter += (_, __) => { btnRegister.BackColor = AccentHover; btnRegister.Invalidate(); };
            btnRegister.MouseLeave += (_, __) => { btnRegister.BackColor = AccentColor; btnRegister.Invalidate(); };
            btnRegister.Click += async (_, __) => await OnRegister();
            card.Controls.Add(btnRegister);

            // ── Login link ──
            var lblLogin = new Label
            {
                Text = "Already have an account?  Sign in",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point((380 - TextRenderer.MeasureText("Already have an account?  Sign in", new Font("Segoe UI", 9.5f)).Width) / 2, 492),
                Cursor = Cursors.Hand
            };
            lblLogin.MouseEnter += (_, __) => lblLogin.ForeColor = AccentColor;
            lblLogin.MouseLeave += (_, __) => lblLogin.ForeColor = MutedText;
            lblLogin.Click += (_, __) => Close();
            card.Controls.Add(lblLogin);

            AcceptButton = btnRegister;
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

        private async Task OnRegister()
        {
            btnRegister.Enabled = false;
            lblError.Visible = false;

            var username = txtUsername.Text.Trim();
            var email = txtEmail.Text.Trim();
            var password = txtPassword.Text;
            var confirm = txtConfirm.Text;

            // Validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirm))
            {
                ShowError("Please fill in all fields.");
                btnRegister.Enabled = true;
                return;
            }

            if (password != confirm)
            {
                ShowError("Passwords do not match.");
                btnRegister.Enabled = true;
                return;
            }

            var (success, error) = await _auth.RegisterAsync(username, email, password);
            btnRegister.Enabled = true;

            if (success)
            {
                MessageBox.Show(
                    "✅  Account created successfully!\n\nYou can now sign in with your credentials.",
                    "Welcome to GameWiki!",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Close();
            }
            else
            {
                ShowError(error ?? "Registration failed.");
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = "  ⚠  " + message;
            lblError.Visible = true;
        }
    }
}