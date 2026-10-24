using Microsoft.Maui.Controls;
using ChessCore.Mobile.ViewModels;

namespace ChessCore.Mobile.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly ProfileViewModel _vm;

        public ProfilePage(ProfileViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try { await _vm.LoadProfileAsync(); } catch (Exception ex) { _vm.ErrorMessage = ex.Message; }
        }
    }
}
