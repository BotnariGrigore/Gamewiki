using System;
using System.Windows.Forms;
using Dapper;
using GameWikiApp.Forms.Auth;

namespace GameWikiApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Map snake_case columns to PascalCase properties for Dapper
            DefaultTypeMap.MatchNamesWithUnderscores = true;

            ApplicationConfiguration.Initialize();
            Application.Run(new AuthForm());
        }
    }
}
