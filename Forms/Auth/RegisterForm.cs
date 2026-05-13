using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Services;
using GameWikiApp.Helpers;

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

        public RegisterForm()
        {
            InitializeForm();
            InitializeControls();
        }

        private void InitializeForm()
        {
            Text = "Nexoria - Create Account";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(460, 620);
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
                Size = new Size(400, 540),
                Location = new Point((ClientSize.Width - 400) / 2, (ClientSize.Height - 540) / 2),
                BackColor = ThemeHelper.BgSecondary,
            };
            card.Paint += (s, e) =>
            {
                using var path = ThemeHelper.GetRoundedPath(card.ClientRectangle, 16);
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
                Text = "🚀",
                Font = new Font("Segoe UI", 40),
                ForeColor = ThemeHelper.Accent,
                AutoSize = true,
                Location = new Point(168, 18)
            };
            card.Controls.Add(lblIcon);

            var lblTitle = ThemeHelper.CreateLabel("Create Account", 22, FontStyle.Bold, ThemeHelper.TextPrimary,
                (card.Width - TextRenderer.MeasureText("Create Account", new Font("Segoe UI", 22, FontStyle.Bold)).Width) / 2, 72);
            card.Controls.Add(lblTitle);

            var lblSubtitle = ThemeHelper.CreateLabel("Join the Nexoria community today", 10, FontStyle.Regular, ThemeHelper.TextSecondary,
                (card.Width - TextRenderer.MeasureText("Join the Nexoria community today", new Font("Segoe UI", 10)).Width) / 2, 104);
            card.Controls.Add(lblSubtitle);

            // Username
            var lblUser = ThemeHelper.CreateLabel("👤  Username", 10, FontStyle.Regular, ThemeHelper.TextSecondary, 30, 148);
            card.Controls.Add(lblUser);

            txtUsername = new TextBox
            {
                PlaceholderText = "Choose a username",
                Location = new Point(30, 173),
                Size = new Size(340, 40),
                BackColor = ThemeHelper.BgInput,
                ForeColor = ThemeHelper.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
            };
            txtUsername.Enter += (_, __) => txtUsername.BackColor = ThemeHelper.BgTertiary;
            txtUsername.Leave += (_, __) => txtUsername.BackColor = ThemeHelper.BgInput;
            var wrap1 = ThemeHelper.WrapInput(txtUsername, 346, 44);
            wrap1.Location = new Point(27, 171);
            card.Controls.Add(wrap1);

            // Email
            var lblEmail = ThemeHelper.CreateLabel("📧  Email", 10, FontStyle.Regular, ThemeHelper.TextSecondary, 30, 222);
            card.Controls.Add(lblEmail);

            txtEmail = new TextBox
            {
                PlaceholderText = "you@example.com",
                Location = new Point(30, 247),
                Size = new Size(340, 40),
                BackColor = ThemeHelper.BgInput,
                ForeColor = ThemeHelper.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
            };
            txtEmail.Enter += (_, __) => txtEmail.BackColor = ThemeHelper.BgTertiary;
            txtEmail.Leave += (_, __) => txtEmail.BackColor = ThemeHelper.BgInput;
            var wrap2 = ThemeHelper.WrapInput(txtEmail, 346, 44);
            wrap2.Location = new Point(27, 245);
            card.Controls.Add(wrap2);

            // Password
            var lblPass = ThemeHelper.CreateLabel("🔒  Password", 10, FontStyle.Regular, ThemeHelper.TextSecondary, 30, 298);
            card.Controls.Add(lblPass);

            txtPassword = new TextBox
            {
                PlaceholderText = "Min 8 chars with A-Z, a-z, 0-9, symbol",
                Location = new Point(30, 323),
                Size = new Size(340, 40),
                BackColor = ThemeHelper.BgInput,
                ForeColor = ThemeHelper.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true
            };
            txtPassword.Enter += (_, __) => txtPassword.BackColor = ThemeHelper.BgTertiary;
            txtPassword.Leave += (_, __) => txtPassword.BackColor = ThemeHelper.BgInput;
            var wrap3 = ThemeHelper.WrapInput(txtPassword, 346, 44);
            wrap3.Location = new Point(27, 321);
            card.Controls.Add(wrap3);

            // Confirm
            var lblConfirm = ThemeHelper.CreateLabel("✅  Confirm Password", 10, FontStyle.Regular, ThemeHelper.TextSecondary, 30, 376);
            card.Controls.Add(lblConfirm);

            txtConfirm = new TextBox
            {
                PlaceholderText = "Repeat password",
                Location = new Point(30, 401),
                Size = new Size(340, 40),
                BackColor = ThemeHelper.BgInput,
                ForeColor = ThemeHelper.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true
            };
            txtConfirm.Enter += (_, __) => txtConfirm.BackColor = ThemeHelper.BgTertiary;
            txtConfirm.Leave += (_, __) => txtConfirm.BackColor = ThemeHelper.BgInput;
            var wrap4 = ThemeHelper.WrapInput(txtConfirm, 346, 44);
            wrap4.Location = new Point(27, 399);
            card.Controls.Add(wrap4);

            // Error
            lblError = new Label
            {
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeHelper.Error,
                AutoSize = true,
                Location = new Point(30, 455),
                Visible = false
            };
            card.Controls.Add(lblError);

            // Register button
            btnRegister = ThemeHelper.CreateThemedButton("Create Account", 30, 472, 340, 44);
            btnRegister.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += async (_, __) => await OnRegister();
            card.Controls.Add(btnRegister);

            // Divider
            var divider = ThemeHelper.CreateSeparator(280, 0, 528);
            divider.Location = new Point((card.Width - 280) / 2, 526);
            card.Controls.Add(divider);

            var lblOr = ThemeHelper.CreateLabel("or", 10, FontStyle.Bold, ThemeHelper.TextMuted,
                (card.Width - TextRenderer.MeasureText("or", new Font("Segoe UI", 10, FontStyle.Bold)).Width) / 2, 536);
            card.Controls.Add(lblOr);

            // Login link
            var lblLogin = ThemeHelper.CreateLabel("Already have an account?  Sign in", 10, FontStyle.Regular, ThemeHelper.TextMuted,
                (card.Width - TextRenderer.MeasureText("Already have an account?  Sign in", new Font("Segoe UI", 10)).Width) / 2, 556);
            lblLogin.Cursor = Cursors.Hand;
            lblLogin.MouseEnter += (_, __) => lblLogin.ForeColor = ThemeHelper.Accent;
            lblLogin.MouseLeave += (_, __) => lblLogin.ForeColor = ThemeHelper.TextMuted;
            lblLogin.Click += (_, __) => Close();
            card.Controls.Add(lblLogin);

            // Password requirements hint
            var hint = ThemeHelper.CreateLabel("⚠ At least 8 characters, 1 uppercase, 1 lowercase, 1 number, 1 special character", 8.5f, FontStyle.Regular, ThemeHelper.TextMuted, 30, 500);
            card.Controls.Add(hint);

            AcceptButton = btnRegister;
        }

        private async Task OnRegister()
        {
            btnRegister.Enabled = false;
            lblError.Visible = false;

            var user = txtUsername.Text.Trim();
            var email = txtEmail.Text.Trim();
            var pass = txtPassword.Text;
            var conf = txtConfirm.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(conf))
            {
                ShowError("Please fill in all fields.");
                btnRegister.Enabled = true;
                return;
            }

            if (pass != conf)
            {
                ShowError("Passwords do not match.");
                btnRegister.Enabled = true;
                return;
            }

            var (success, err) = await _auth.RegisterAsync(user, email, pass);
            btnRegister.Enabled = true;

            if (success)
            {
                MessageBox.Show(
                    "✅  Account created successfully!\n\nYou can now sign in with your credentials.",
                    "Welcome to Nexoria!",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Close();
            }
            else
                ShowError(err ?? "Registration failed.");
        }

        private void ShowError(string message)
        {
            lblError.Text = "  ⚠  " + message;
            lblError.Visible = true;
        }
    }
}