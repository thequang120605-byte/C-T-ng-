using Microsoft.Maui.Controls;
using ChessCore.Mobile.ViewModels;

namespace ChessCore.Mobile.Views
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage(RegisterViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
