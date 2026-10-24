using Microsoft.Maui.Controls;
using ChessCore.Mobile.ViewModels;

namespace ChessCore.Mobile.Views
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage(SettingsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
