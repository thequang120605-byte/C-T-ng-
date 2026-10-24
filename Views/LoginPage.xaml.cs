using Microsoft.Maui.Controls;
using ChessCore.Mobile.ViewModels;

namespace ChessCore.Mobile.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
