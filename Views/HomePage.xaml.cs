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
    }
}
