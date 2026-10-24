using System;
using Microsoft.Maui.Controls;
using ChessCore.Mobile.ViewModels;

namespace ChessCore.Mobile.Views
{
    public partial class HomePage : ContentPage
    {
        private readonly HomeViewModel _vm;

        public HomePage(HomeViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try { await _vm.LoadUserAsync(); } catch (Exception ex) { _vm.ErrorMessage = ex.Message; }
        }

        private void OnDifficulty1Clicked(object sender, EventArgs e) => _vm.SelectedDifficulty = 1;
        private void OnDifficulty2Clicked(object sender, EventArgs e) => _vm.SelectedDifficulty = 2;
        private void OnDifficulty3Clicked(object sender, EventArgs e) => _vm.SelectedDifficulty = 3;
        private void OnDifficulty4Clicked(object sender, EventArgs e) => _vm.SelectedDifficulty = 4;
    }
}
