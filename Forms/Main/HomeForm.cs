using System.Drawing;
using System.Windows.Forms;

namespace GameWikiApp.Forms.Main
{
    public class HomeForm : Form
    {
        public HomeForm(string username)
        {
            Text = "GameWikiApp - Home";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(800, 600);

            var lbl = new Label
            {
                Text = $"Welcome, {username}",
                Font = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true
            };

            Controls.Add(lbl);
        }
    }
}
