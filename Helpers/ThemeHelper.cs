using System;
using System.Windows;

namespace GameWikiApp.Helpers
{
    public static class ThemeHelper
    {
        public static void SetTheme(bool isDarkMode)
        {
            if (isDarkMode)
            {
                // Apply dark mode styles
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/Styles/DarkTheme.xaml")
                });
            }
            else
            {
                // Apply light mode styles
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/Styles/LightTheme.xaml")
                });
            }
        }
    }
}