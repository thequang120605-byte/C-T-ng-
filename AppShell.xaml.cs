using Microsoft.Maui.Controls;
using ChessCore.Mobile.Views;

namespace ChessCore.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("register", typeof(RegisterPage));
            Routing.RegisterRoute("game", typeof(GamePage));
            Routing.RegisterRoute("settings", typeof(SettingsPage));
            Routing.RegisterRoute("puzzle", typeof(PuzzlePage));
            Routing.RegisterRoute("course", typeof(CoursePage));
            Routing.RegisterRoute("history", typeof(HistoryPage));
        }
    }
}
