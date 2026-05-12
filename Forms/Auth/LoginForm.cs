using System;
using System.Drawing;
using System.IO;
using GameWikiApp.UI.Controls;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Services;

namespace GameWikiApp.Forms.Auth
{
    public class LoginForm : Form
    {
        private IconTextBox txtUsername = new() { Width = 320 };
        private IconTextBox txtPassword = new() { Width = 320 };
        private RoundedButton btnLogin = new() { Text = "Sign In", Width = 260, Height = 36, BackColor = Color.FromArgb(70, 130, 180), ForeColor = Color.White };
        private RoundedButton btnRegister = new() { Text = "Register", Width = 260, Height = 36, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White };

        private readonly AuthService _auth = new();

        public LoginForm()
        {
            Text = "GameWikiApp";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 520);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // Left image area
            var left = new Panel { Dock = DockStyle.Left, Width = 360, BackColor = Color.Black };
            var imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", "auth_bg.jpg");
            if (File.Exists(imgPath))
            {
                left.BackgroundImage = Image.FromFile(imgPath);
                left.BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
            {
                left.BackColor = Color.FromArgb(40, 40, 40);
            }

            // Right panel with form
            var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(45, 50, 60) };

            var title = new Label { Text = "LOG IN", ForeColor = Color.White, Font = new Font("Segoe UI", 24, FontStyle.Bold), AutoSize = true };
            title.Location = new Point(60, 60);

            var lblUser = new Label { Text = "User Name", ForeColor = Color.LightGray, Location = new Point(60, 130), AutoSize = true };
            txtUsername.Location = new Point(60, 155);
            txtUsername.SetIcon(IconTextBox.IconType.User);

            var lblPass = new Label { Text = "Password", ForeColor = Color.LightGray, Location = new Point(60, 205), AutoSize = true };
            txtPassword.Location = new Point(60, 230);
            txtPassword.SetIcon(IconTextBox.IconType.Lock);
            txtPassword.UseSystemPasswordChar = true;

            btnLogin.Location = new Point(60, 290);
            btnLogin.Click += async (_, __) => await OnLogin();

            btnRegister.Location = new Point(60, 340);
            btnRegister.Click += (_, __) => OpenRegister();

            right.Controls.AddRange(new Control[] { title, lblUser, txtUsername, lblPass, txtPassword, btnLogin, btnRegister });

            Controls.AddRange(new Control[] { left, right });
        }

        private async Task OnLogin()
        {
            btnLogin.Enabled = false;
            var user = await _auth.AuthenticateAsync(txtUsername.Value.Trim(), txtPassword.Value);
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
                MessageBox.Show("Invalid credentials.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenRegister()
        {
            using var reg = new RegisterForm();
            reg.ShowDialog(this);
        }
    }
}
