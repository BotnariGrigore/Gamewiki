using System;
using System.Drawing;
using System.IO;
using GameWikiApp.UI.Controls;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Services;

namespace GameWikiApp.Forms.Auth
{
    public class RegisterForm : Form
    {
        private IconTextBox txtUsername = new() { Width = 320 };
        private IconTextBox txtEmail = new() { Width = 320 };
        private IconTextBox txtPassword = new() { Width = 320 };
        private IconTextBox txtConfirm = new() { Width = 320 };
        private RoundedButton btnRegister = new() { Text = "Register", Width = 260, Height = 36, BackColor = Color.FromArgb(70, 130, 180), ForeColor = Color.White };

        private readonly AuthService _auth = new();

        public RegisterForm()
        {
            Text = "Register";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(900, 520);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

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

            var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(45, 50, 60) };

            var title = new Label { Text = "REGISTER", ForeColor = Color.White, Font = new Font("Segoe UI", 24, FontStyle.Bold), AutoSize = true };
            title.Location = new Point(60, 40);

            var lblUser = new Label { Text = "User Name", ForeColor = Color.LightGray, Location = new Point(60, 120), AutoSize = true };
            txtUsername.Location = new Point(60, 145);
            txtUsername.SetIcon(IconTextBox.IconType.User);

            var lblEmail = new Label { Text = "Mail", ForeColor = Color.LightGray, Location = new Point(60, 190), AutoSize = true };
            txtEmail.Location = new Point(60, 215);
            // no icon for email by default

            var lblPass = new Label { Text = "Password", ForeColor = Color.LightGray, Location = new Point(60, 260), AutoSize = true };
            txtPassword.Location = new Point(60, 285);
            txtPassword.SetIcon(IconTextBox.IconType.Lock);
            txtPassword.UseSystemPasswordChar = true;

            var lblConf = new Label { Text = "Confirm Password", ForeColor = Color.LightGray, Location = new Point(60, 330), AutoSize = true };
            txtConfirm.Location = new Point(60, 355);
            txtConfirm.SetIcon(IconTextBox.IconType.Lock);
            txtConfirm.UseSystemPasswordChar = true;

            btnRegister.Location = new Point(60, 410);
            btnRegister.BackColor = Color.FromArgb(70, 130, 180);
            btnRegister.ForeColor = Color.White;
            btnRegister.Click += async (_, __) => await OnRegister();

            right.Controls.AddRange(new Control[] { title, lblUser, txtUsername, lblEmail, txtEmail, lblPass, txtPassword, lblConf, txtConfirm, btnRegister });

            Controls.AddRange(new Control[] { left, right });
        }

        private async Task OnRegister()
        {
            btnRegister.Enabled = false;
            var user = txtUsername.Value.Trim();
            var email = txtEmail.Value.Trim();
            var pass = txtPassword.Value;
            var conf = txtConfirm.Value;

            if (pass != conf)
            {
                MessageBox.Show("Passwords do not match.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnRegister.Enabled = true;
                return;
            }

            var (success, error) = await _auth.RegisterAsync(user, email, pass);
            btnRegister.Enabled = true;
            if (success)
            {
                MessageBox.Show("Registered successfully. You can now log in.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            else
            {
                MessageBox.Show(error ?? "Registration failed.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
