using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Dapper;
using GameWikiApp.Helpers;
using GameWikiApp.Forms.Auth;

namespace GameWikiApp
{
    /// <summary>
    /// Extension to add placeholder text support to WinForms TextBox.
    /// Uses native SendMessage with EM_SETCUEBANNER on Windows.
    /// </summary>
    public static class PlaceholderTextExtension
    {
        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, string lp);

        public static void SetPlaceholder(this TextBox textBox, string text)
        {
            if (Application.RenderWithVisualStyles)
            {
                SendMessage(textBox.Handle, EM_SETCUEBANNER, IntPtr.Zero, text);
            }
            else
            {
                textBox.GotFocus += (s, e) =>
                {
                    if (textBox.Text == text && textBox.ForeColor == Color.Gray)
                    {
                        textBox.Text = "";
                        textBox.ForeColor = SystemColors.WindowText;
                    }
                };
                textBox.LostFocus += (s, e) =>
                {
                    if (string.IsNullOrEmpty(textBox.Text))
                    {
                        textBox.Text = text;
                        textBox.ForeColor = Color.Gray;
                    }
                };
                textBox.Text = text;
                textBox.ForeColor = Color.Gray;
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;

            ApplicationConfiguration.Initialize();

            // Apply dark theme by default
            ThemeHelper.CurrentTheme = ThemeHelper.ThemeMode.Dark;

            Application.Run(new AuthForm());
        }
    }
}