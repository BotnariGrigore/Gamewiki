using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Services;
using GameWikiApp.Helpers;

namespace GameWikiApp.Forms.Auth
{
    public class AuthForm : Form
    {
        private bool _isLoginMode = true;

        // Login fields
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private TextBox txtRegUser = null!;
        private TextBox txtRegEmail = null!;
        private TextBox txtRegPass = null!;
        private TextBox txtRegConfirm = null!;

        private Button btnSubmit = null!;
        private Label lblError;
        private Label lblTabLogin;
        private Label lblTabRegister;
        private Panel loginPanel;
        private Panel registerPanel;
        private Label lblStatus;

        private readonly AuthService _auth = new();

        public AuthForm()
        {
            Text = "Nexoria";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(960, 640);
            MinimumSize = new Size(850, 550);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = ThemeHelper.BgPrimary;
            Font = new Font("Segoe UI", 10);

            ThemeHelper.ApplyTheme(this);
            BuildLeftPanel();
            BuildRightPanel();
            ToggleMode();
            _ = CheckDbAndShowStatusAsync();
        }

        private void BuildLeftPanel()
        {
            var left = new Panel
            {
                Size = new Size(300, ClientSize.Height),
                Location = new Point(0, 0),
                BackColor = ThemeHelper.Sidebar,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom
            };
            left.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, left.Width, left.Height);
                using var lg = new LinearGradientBrush(rect,
                    ThemeHelper.Dark_BgSecondary, ThemeHelper.Dark_BgPrimary, LinearGradientMode.Vertical);
                g.FillRectangle(lg, rect);

                // Game icon
                var iconFont = new Font("Segoe UI", 48);
                var iconSz = g.MeasureString("🎮", iconFont);
                g.DrawString("🎮", iconFont, Brushes.White, (left.Width - iconSz.Width) / 2, 120);

                // Title - Nexoria
                var tFont = new Font("Segoe UI", 24, FontStyle.Bold);
                var tSz = g.MeasureString("Nexoria", tFont);
                g.DrawString("Nexoria", tFont, Brushes.White, (left.Width - tSz.Width) / 2, 200);
            };
            Controls.Add(left);
        }

        private void BuildRightPanel()
        {
            var right = new Panel
            {
                Location = new Point(300, 0),
                Size = new Size(ClientSize.Width - 300, ClientSize.Height),
                BackColor = ThemeHelper.BgPrimary,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(right);

            // Header welcome text
            var lblWelcome = ThemeHelper.CreateLabel("Welcome to Nexoria!", 22, FontStyle.Bold, ThemeHelper.TextPrimary, 40, 50);
            right.Controls.Add(lblWelcome);

            var lblSub = ThemeHelper.CreateLabel("Sign in to your account or create a new one to continue.", 10, FontStyle.Regular, ThemeHelper.TextPrimary, 40, 82);
            right.Controls.Add(lblSub);

            // Tabs
            lblTabLogin = new Label
            {
                Text = "Sign In",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = ThemeHelper.Accent,
                AutoSize = false,
                Size = new Size(140, 38),
                Location = new Point(40, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblTabLogin.Click += (_, __) => { if (!_isLoginMode) { _isLoginMode = true; ToggleMode(); } };
            right.Controls.Add(lblTabLogin);

            lblTabRegister = new Label
            {
                Text = "Create Account",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = false,
                Size = new Size(160, 38),
                Location = new Point(200, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblTabRegister.Click += (_, __) => { if (_isLoginMode) { _isLoginMode = false; ToggleMode(); } };
            right.Controls.Add(lblTabRegister);

            // Tab underline indicator
            var lblIndicator = new Label
            {
                Name = "indicator",
                Size = new Size(140, 3),
                Location = new Point(40, 158),
                BackColor = ThemeHelper.Accent
            };
            right.Controls.Add(lblIndicator);
            _indicator = lblIndicator;

            right.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var active = _isLoginMode ? lblTabLogin : lblTabRegister;
                var activeX = active.Location.X;
                var activeW = active.Width;
                g.FillRectangle(new SolidBrush(ThemeHelper.Accent), activeX, active.Bottom, activeW, 3);
            };

            // ── Login Panel ──
            loginPanel = new Panel
            {
                Location = new Point(40, 170),
                Size = new Size(right.Width - 80, right.Height - 200),
                BackColor = ThemeHelper.BgPrimary
            };

            AddLoginControls(loginPanel);
            right.Controls.Add(loginPanel);

            // ── Register Panel ──
            registerPanel = new Panel
            {
                Location = new Point(40, 170),
                Size = new Size(right.Width - 80, right.Height - 200),
                BackColor = ThemeHelper.BgPrimary,
                Visible = false
            };

            AddRegisterControls(registerPanel);
            right.Controls.Add(registerPanel);

            // Create a single shared submit button used for both modes
            btnSubmit = ThemeHelper.CreateThemedButton("Sign In", 0, 200, loginPanel.Width - 4, 40);
            btnSubmit.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnSubmit.Click += OnSubmit;
            loginPanel.Controls.Add(btnSubmit);
            AcceptButton = btnSubmit;

            // Shared error label used by both login and register panels
            lblError = ThemeHelper.CreateLabel("", 9, FontStyle.Regular, ThemeHelper.Error, 0, 180);
            lblError.Visible = false;
            loginPanel.Controls.Add(lblError);

            // Status label
            lblStatus = ThemeHelper.CreateLabel("", 9, FontStyle.Regular, ThemeHelper.Success, 40, right.Height - 30);
            right.Controls.Add(lblStatus);
        }

        private Label _indicator = null!;

        private void AddLoginControls(Panel p)
        {
            var lblUser = ThemeHelper.CreateLabel("Username or Email", 10, FontStyle.Bold, ThemeHelper.TextPrimary, 0, 20);
            p.Controls.Add(lblUser);

            txtUsername = new TextBox
            {
                PlaceholderText = "Enter username or email",
                Size = new Size(p.Width - 8, 40),
                Font = new Font("Segoe UI", 11)
            };
            var wrapUser = ThemeHelper.WrapInput(txtUsername, p.Width - 4, 44);
            wrapUser.Location = new Point(0, 42);
            p.Controls.Add(wrapUser);

            var lblPass = ThemeHelper.CreateLabel("Password", 10, FontStyle.Bold, ThemeHelper.TextPrimary, 0, 100);
            p.Controls.Add(lblPass);

            txtPassword = new TextBox
            {
                PlaceholderText = "Enter password",
                Size = new Size(p.Width - 8, 40),
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true
            };
            var wrapPass = ThemeHelper.WrapInput(txtPassword, p.Width - 4, 44);
            wrapPass.Location = new Point(0, 122);
            p.Controls.Add(wrapPass);

            var link = ThemeHelper.CreateLabel("Forgot password?", 9, FontStyle.Underline, ThemeHelper.Accent, 0, 250);
            link.Cursor = Cursors.Hand;
            link.Click += (_, __) => MessageBox.Show("Contact admin to reset your password.", "Password Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
            p.Controls.Add(link);
        }

        private void AddRegisterControls(Panel p)
        {
            var lblUser = ThemeHelper.CreateLabel("Username", 10, FontStyle.Bold, ThemeHelper.TextPrimary, 0, 10);
            p.Controls.Add(lblUser);

            txtRegUser = new TextBox
            {
                PlaceholderText = "Choose username",
                Size = new Size(p.Width - 8, 40),
                Font = new Font("Segoe UI", 11)
            };
            var w1 = ThemeHelper.WrapInput(txtRegUser, p.Width - 4, 44);
            w1.Location = new Point(0, 32);
            p.Controls.Add(w1);

            var lblEmail = ThemeHelper.CreateLabel("Email", 10, FontStyle.Bold, ThemeHelper.TextPrimary, 0, 86);
            p.Controls.Add(lblEmail);

            txtRegEmail = new TextBox
            {
                PlaceholderText = "Enter email",
                Size = new Size(p.Width - 8, 40),
                Font = new Font("Segoe UI", 11)
            };
            var w2 = ThemeHelper.WrapInput(txtRegEmail, p.Width - 4, 44);
            w2.Location = new Point(0, 108);
            p.Controls.Add(w2);

            var lblPass = ThemeHelper.CreateLabel("Password", 10, FontStyle.Bold, ThemeHelper.TextPrimary, 0, 160);
            p.Controls.Add(lblPass);

            txtRegPass = new TextBox
            {
                PlaceholderText = "Create password (8+ chars, upper, lower, number, symbol)",
                Size = new Size(p.Width - 8, 40),
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true
            };
            var w3 = ThemeHelper.WrapInput(txtRegPass, p.Width - 4, 44);
            w3.Location = new Point(0, 182);
            p.Controls.Add(w3);

            var lblConf = ThemeHelper.CreateLabel("Confirm Password", 10, FontStyle.Bold, ThemeHelper.TextPrimary, 0, 232);
            p.Controls.Add(lblConf);

            txtRegConfirm = new TextBox
            {
                PlaceholderText = "Repeat password",
                Size = new Size(p.Width - 8, 40),
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true
            };
            var w4 = ThemeHelper.WrapInput(txtRegConfirm, p.Width - 4, 44);
            w4.Location = new Point(0, 254);
            p.Controls.Add(w4);

            // Password requirements hint
            var hint = ThemeHelper.CreateLabel("⚠ 8+ chars | A-Z | a-z | 0-9 | Special char", 8, FontStyle.Regular, ThemeHelper.TextMuted, 0, 305);
            p.Controls.Add(hint);
        }

        private void ToggleMode()
        {
            _indicator.Location = new Point(_isLoginMode ? 40 : 200, 158);
            _indicator.Size = new Size(_isLoginMode ? 140 : 160, 3);

            lblTabLogin.ForeColor = _isLoginMode ? ThemeHelper.Accent : ThemeHelper.TextMuted;
            lblTabRegister.ForeColor = _isLoginMode ? ThemeHelper.TextMuted : ThemeHelper.Accent;
            loginPanel.Visible = _isLoginMode;
            registerPanel.Visible = !_isLoginMode;

            if (!btnSubmit.IsDisposed)
            {
                btnSubmit.Text = _isLoginMode ? "Sign In" : "Create Account";

                // Move button to correct panel
                btnSubmit.Parent?.Controls.Remove(btnSubmit);
                var parent = _isLoginMode ? loginPanel : registerPanel;
                parent.Controls.Add(btnSubmit);
                btnSubmit.Location = new Point(0, _isLoginMode ? 200 : 355);
                btnSubmit.Size = new Size(parent.Width - 4, 40);

                lblError.Parent?.Controls.Remove(lblError);
                parent.Controls.Add(lblError);
                lblError.Location = new Point(0, _isLoginMode ? 180 : 330);
                lblError.Visible = false;
            }

            Invalidate();
        }

        private TextBox? GetRegTextBox(string name)
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

        private async void OnSubmit(object? sender, EventArgs e)
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

            var (result, error) = await _auth.AuthenticateAsync(user, pass);
            btnSubmit.Enabled = true;

            if (error != null)
            {
                ShowError(error);
                return;
            }

            if (result != null)
            {
                Helpers.SessionManager.StartSession(result, Guid.NewGuid().ToString());
                Hide();
                var home = new Main.HomeForm();
                home.FormClosed += (_, __) => { Environment.Exit(0); };
                home.Show();
                lblStatus.Text = string.Empty;
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

            var user = txtRegUser.Text.Trim();
            var email = txtRegEmail.Text.Trim();
            var pass = txtRegPass.Text;
            var conf = txtRegConfirm.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(conf))
            {
                ShowError("Please fill in all fields."); btnSubmit.Enabled = true; return;
            }
            if (pass != conf)
            {
                ShowError("Passwords do not match."); btnSubmit.Enabled = true; return;
            }

            var (success, err) = await _auth.RegisterAsync(user, email, pass);
            btnSubmit.Enabled = true;
            if (success)
            {
                lblStatus.Text = "✓ Account created! You can now sign in.";
                lblStatus.ForeColor = ThemeHelper.Success;
                txtUsername.Text = user;
                _isLoginMode = true;
                ToggleMode();
                txtPassword.Focus();
            }
            else
                ShowError(err ?? "Registration failed.");
        }

        private void ShowError(string msg)
        {
            lblError.Text = "⚠  " + msg;
            lblError.Visible = true;
        }

        private async Task CheckDbAndShowStatusAsync()
        {
            var err = await _auth.CheckDatabaseAsync();
            if (err != null)
            {
                lblStatus.Text = "Database connection error: " + err;
                lblStatus.ForeColor = ThemeHelper.Error;
            }
            else
            {
                lblStatus.Text = "Connected to database.";
                lblStatus.ForeColor = ThemeHelper.Success;
            }
        }
    }
}