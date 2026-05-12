using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Services;

namespace GameWikiApp.Forms.Auth
{
    public class AuthForm : Form
    {
        private bool _isLoginMode = true;

        // Login fields
        private TextBox txtUsername;
        private TextBox txtPassword;
        // Register fields (found by name)
        private readonly string[] _regNames = { "txtRegUser", "txtRegEmail", "txtRegPass", "txtRegConfirm" };

        private Button btnSubmit;
        private Label lblError;
        private Label lblTabLogin;
        private Label lblTabRegister;
        private Panel loginPanel;
        private Panel registerPanel;

        private readonly AuthService _auth = new();

        private readonly Color DarkPanel = Color.FromArgb(38, 38, 38);
        private readonly Color WhiteBg = Color.White;
        private readonly Color InputBg = Color.FromArgb(245, 245, 245);
        private readonly Color InputBorder = Color.FromArgb(220, 220, 220);
        private readonly Color PrimaryBlack = Color.FromArgb(30, 30, 30);
        private readonly Color GrayText = Color.FromArgb(150, 150, 150);
        private readonly Color DarkGrayText = Color.FromArgb(100, 100, 100);

        public AuthForm()
        {
            Text = "GameWiki";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 600);
            MinimumSize = new Size(800, 550);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = WhiteBg;
            Font = new Font("Segoe UI", 10);

            BuildLeftPanel();
            BuildRightPanel();
            ToggleMode();
        }

        private void BuildLeftPanel()
        {
            var left = new Panel
            {
                Size = new Size(250, ClientSize.Height),
                Location = new Point(0, 0),
                BackColor = DarkPanel
            };
            left.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var lg = new LinearGradientBrush(new Rectangle(0, 0, left.Width, left.Height),
                    Color.FromArgb(38, 38, 38), Color.FromArgb(20, 20, 20), LinearGradientMode.Vertical);
                g.FillRectangle(lg, left.ClientRectangle);

                // Game icon
                var iconFont = new Font("Segoe UI", 42);
                var iconSz = g.MeasureString("🎮", iconFont);
                g.DrawString("🎮", iconFont, Brushes.White, (left.Width - iconSz.Width) / 2, 130);

                // Title
                var tFont = new Font("Segoe UI", 20, FontStyle.Bold);
                var tSz = g.MeasureString("GameWiki", tFont);
                g.DrawString("GameWiki", tFont, Brushes.White, (left.Width - tSz.Width) / 2, 200);

                // Subtitle
                var sFont = new Font("Segoe UI", 10);
                var sSz = g.MeasureString("Your gaming encyclopedia", sFont);
                g.DrawString("Your gaming encyclopedia", sFont, Brushes.Gray, (left.Width - sSz.Width) / 2, 234);

                // Line
                g.FillRectangle(Brushes.DimGray, (left.Width - 50) / 2, 262, 50, 2);

                // Tagline
                var tagFont = new Font("Segoe UI", 8);
                var tagSz = g.MeasureString("© 2026 GameWiki Community", tagFont);
                g.DrawString("© 2026 GameWiki Community", tagFont, Brushes.DimGray, (left.Width - tagSz.Width) / 2, left.Height - 40);
            };
            Controls.Add(left);
        }

        private void BuildRightPanel()
        {
            // ── Right container ──
            var right = new Panel
            {
                Size = new Size(650, ClientSize.Height),
                Location = new Point(250, 0),
                BackColor = WhiteBg
            };
            Controls.Add(right);

            // ── Tabs ──
            lblTabLogin = new Label
            {
                Text = "Sign In",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = PrimaryBlack,
                AutoSize = false,
                Size = new Size(130, 34),
                Location = new Point(50, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblTabLogin.Click += (_, __) => { if (!_isLoginMode) { _isLoginMode = true; ToggleMode(); } };
            right.Controls.Add(lblTabLogin);

            lblTabRegister = new Label
            {
                Text = "Sign Up",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = GrayText,
                AutoSize = false,
                Size = new Size(130, 34),
                Location = new Point(190, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblTabRegister.Click += (_, __) => { if (_isLoginMode) { _isLoginMode = false; ToggleMode(); } };
            right.Controls.Add(lblTabRegister);

            // ── Tab underline (painted) ──
            right.Paint += (s, e) =>
            {
                var active = _isLoginMode ? lblTabLogin : lblTabRegister;
                e.Graphics.FillRectangle(new SolidBrush(PrimaryBlack), active.Left + 20, active.Bottom, active.Width - 40, 3);
            };

            // ── Login panel ──
            loginPanel = new Panel
            {
                Size = new Size(400, 450),
                Location = new Point(50, 110),
                BackColor = WhiteBg
            };
            AddLoginControls(loginPanel);
            right.Controls.Add(loginPanel);

            // ── Register panel ──
            registerPanel = new Panel
            {
                Size = new Size(400, 450),
                Location = new Point(50, 110),
                BackColor = WhiteBg,
                Visible = false
            };
            AddRegisterControls(registerPanel);
            right.Controls.Add(registerPanel);
        }

        private void AddLoginControls(Panel p)
        {
            p.Controls.Add(MakeLabel("Welcome back", 22, FontStyle.Bold, PrimaryBlack, 0, 0));
            p.Controls.Add(MakeLabel("Enter your credentials to access your account", 10, FontStyle.Regular, DarkGrayText, 0, 36));

            p.Controls.Add(MakeLabel("Username", 9.5f, FontStyle.Regular, PrimaryBlack, 0, 80));
            txtUsername = new TextBox
            {
                Location = new Point(0, 104), Size = new Size(400, 42), BackColor = InputBg,
                ForeColor = PrimaryBlack, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11)
            };
            AddRoundedInput(p, txtUsername);

            p.Controls.Add(MakeLabel("Password", 9.5f, FontStyle.Regular, PrimaryBlack, 0, 170));
            txtPassword = new TextBox
            {
                Location = new Point(0, 194), Size = new Size(400, 42), BackColor = InputBg,
                ForeColor = PrimaryBlack, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true
            };
            AddRoundedInput(p, txtPassword);

            // Error label
            lblError = MakeLabel("", 9, FontStyle.Regular, Color.FromArgb(220, 53, 69), 0, 256);
            lblError.Visible = false;
            p.Controls.Add(lblError);

            // Button
            btnSubmit = MakeButton("Sign In", 270);
            p.Controls.Add(btnSubmit);

            // Focus events on login textboxes to clear error
            txtUsername.TextChanged += (_, __) => lblError.Visible = false;
            txtPassword.TextChanged += (_, __) => lblError.Visible = false;
        }

        private void AddRegisterControls(Panel p)
        {
            p.Controls.Add(MakeLabel("Create account", 22, FontStyle.Bold, PrimaryBlack, 0, 0));
            p.Controls.Add(MakeLabel("Get started with your free account", 10, FontStyle.Regular, DarkGrayText, 0, 36));

            string[] labels = { "Username", "Email", "Password", "Confirm Password" };
            string[] names = { "txtRegUser", "txtRegEmail", "txtRegPass", "txtRegConfirm" };
            bool[] passwords = { false, false, true, true };
            int y = 80;

            for (int i = 0; i < 4; i++)
            {
                p.Controls.Add(MakeLabel(labels[i], 9.5f, FontStyle.Regular, PrimaryBlack, 0, y));
                var tb = new TextBox
                {
                    Location = new Point(0, y + 24), Size = new Size(400, 42), BackColor = InputBg,
                    ForeColor = PrimaryBlack, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11),
                    UseSystemPasswordChar = passwords[i], Name = names[i]
                };
                AddRoundedInput(p, tb);
                y += 74;
            }

            // Error label (reuse the same one from login - shared)
            // Button text changes based on mode
        }

        // ── Helpers ──
        private Label MakeLabel(string text, float fontSize, FontStyle style, Color color, int x, int y)
        {
            return new Label
            {
                Text = text, Font = new Font("Segoe UI", fontSize, style), ForeColor = color,
                AutoSize = true, Location = new Point(x, y)
            };
        }

        private Button MakeButton(string text, int y)
        {
            var btn = new Button
            {
                Text = text, Location = new Point(0, y), Size = new Size(400, 48),
                FlatStyle = FlatStyle.Flat, BackColor = PrimaryBlack, ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btn.Paint += (s, e) =>
            {
                var b = (Button)s;
                using var path = GetRoundedPath(new Rectangle(0, 0, b.Width - 1, b.Height - 1), 8);
                using var brush = new SolidBrush(PrimaryBlack);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, b.Text, b.Font, b.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btn.MouseEnter += (_, __) => btn.BackColor = Color.FromArgb(60, 60, 60);
            btn.MouseLeave += (_, __) => btn.BackColor = PrimaryBlack;
            btn.Click += async (_, __) => await OnSubmit();
            return btn;
        }

        private void AddRoundedInput(Control parent, TextBox tb)
        {
            var wrapper = new Panel
            {
                Location = tb.Location, Size = tb.Size, BackColor = tb.BackColor
            };
            wrapper.Paint += (s, e) =>
            {
                var r = new Rectangle(0, 0, wrapper.Width - 1, wrapper.Height - 1);
                using var path = GetRoundedPath(r, 8);
                using var brush = new SolidBrush(wrapper.BackColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
                using var pen = new Pen(InputBorder, 1);
                e.Graphics.DrawPath(pen, path);
            };
            tb.Location = new Point(14, 11);
            tb.Size = new Size(wrapper.Width - 28, wrapper.Height - 22);
            tb.BackColor = wrapper.BackColor;
            parent.Controls.Add(wrapper);
            wrapper.Controls.Add(tb);
        }

        private GraphicsPath GetRoundedPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ToggleMode()
        {
            lblTabLogin.ForeColor = _isLoginMode ? PrimaryBlack : GrayText;
            lblTabRegister.ForeColor = _isLoginMode ? GrayText : PrimaryBlack;
            loginPanel.Visible = _isLoginMode;
            registerPanel.Visible = !_isLoginMode;
            btnSubmit.Text = _isLoginMode ? "Sign In" : "Create Account";
            lblError.Visible = false;
            // Add button to visible panel
            var parent = _isLoginMode ? loginPanel : registerPanel;
            if (btnSubmit.Parent != parent)
            {
                btnSubmit.Parent?.Controls.Remove(btnSubmit);
                parent.Controls.Add(btnSubmit);
                btnSubmit.Location = new Point(0, 340);
            }
            // Move error label too
            if (lblError.Parent != parent)
            {
                lblError.Parent?.Controls.Remove(lblError);
                parent.Controls.Add(lblError);
                lblError.Location = new Point(0, 320);
            }
            Invalidate();
            loginPanel.Parent?.Invalidate();
        }

        private TextBox GetRegTextBox(string name)
        {
            foreach (Control c in registerPanel.Controls)
            {
                if (c is Panel p)
                    foreach (Control cc in p.Controls)
                        if (cc is TextBox t && t.Name == name) return t;
                if (c is TextBox t2 && t2.Name == name) return t2;
            }
            return null;
        }

        private async Task OnSubmit()
        {
            if (_isLoginMode) await OnLogin();
            else await OnRegister();
        }

        private async Task OnLogin()
        {
            btnSubmit.Enabled = false;
            lblError.Visible = false;

            var user = txtUsername.Text.Trim();
            var pass = txtPassword.Text;
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowError("Please enter username and password."); btnSubmit.Enabled = true; return;
            }

            var result = await _auth.AuthenticateAsync(user, pass);
            btnSubmit.Enabled = true;
            if (result != null)
            {
                Helpers.SessionManager.StartSession(result, Guid.NewGuid().ToString());
                Hide();
                var home = new Main.HomeForm(result.Username);
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

        private async Task OnRegister()
        {
            btnSubmit.Enabled = false;
            lblError.Visible = false;

            var u = GetRegTextBox("txtRegUser");
            var e = GetRegTextBox("txtRegEmail");
            var p = GetRegTextBox("txtRegPass");
            var c = GetRegTextBox("txtRegConfirm");

            if (u == null || e == null || p == null || c == null)
            { ShowError("Form error."); btnSubmit.Enabled = true; return; }

            var user = u.Text.Trim();
            var email = e.Text.Trim();
            var pass = p.Text;
            var conf = c.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(conf))
            { ShowError("Please fill in all fields."); btnSubmit.Enabled = true; return; }

            if (pass != conf)
            { ShowError("Passwords do not match."); btnSubmit.Enabled = true; return; }

            var (success, err) = await _auth.RegisterAsync(user, email, pass);
            btnSubmit.Enabled = true;
            if (success)
            {
                MessageBox.Show("Account created! You can now sign in.", "Welcome!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtUsername.Text = user;
                _isLoginMode = true;
                ToggleMode();
                txtPassword.Focus();
            }
            else ShowError(err ?? "Registration failed.");
        }

        private void ShowError(string msg)
        {
            lblError.Text = "⚠  " + msg;
            lblError.Visible = true;
        }
    }
}