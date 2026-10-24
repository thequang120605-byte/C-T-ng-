using System.ComponentModel;
using ChessCore.Mobile.ViewModels;

namespace ChessCore.Mobile.Views;

public partial class CoursePage : ContentPage
{
    private readonly CourseViewModel _vm;

    public CoursePage(CourseViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        _vm.PropertyChanged += OnVmPropertyChanged;
        RenderOptions();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(_vm.Options) or nameof(_vm.CurrentIndex) or nameof(_vm.SelectedOptionIndex) or nameof(_vm.IsSubmitted))
        {
            RenderOptions();
        }
    }

    private void RenderOptions()
    {
        OptionsContainer.Children.Clear();
        var options = _vm.Options;
        if (options == null) return;

        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            int index = opt.Index;

            var bg = _vm.GetOptionBackground(index);
            var textClr = _vm.GetOptionTextColor(index);

            var border = new Border
            {
                BackgroundColor = bg,
                Stroke = Color.FromArgb("#D2BF9E"),
                StrokeThickness = _vm.SelectedOptionIndex == index ? 2 : 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(14, 12),
                HeightRequest = 50,
                MinimumHeightRequest = 48
            };

            var label = new Label
            {
                Text = opt.Text,
                TextColor = textClr,
                FontSize = 13,
                FontAttributes = _vm.SelectedOptionIndex == index ? FontAttributes.Bold : FontAttributes.None,
                VerticalOptions = LayoutOptions.Center
            };

            border.Content = label;

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => _vm.SelectOptionCommand.Execute(index);
            border.GestureRecognizers.Add(tap);

            OptionsContainer.Children.Add(border);
        }
    }
}
