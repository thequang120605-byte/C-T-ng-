using ChessCore.Mobile.Models;
using ChessCore.Mobile.ViewModels;
namespace ChessCore.Mobile.Views;
public partial class OnlineLobbyPage : ContentPage
{
    private readonly OnlineLobbyViewModel _vm;
    public OnlineLobbyPage(OnlineLobbyViewModel vm) { InitializeComponent(); _vm = vm; BindingContext = vm; }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { await _vm.LoadRoomsAsync(); } catch (Exception ex) { _vm.ErrorMessage = ex.Message; }
    }
    private void OnJoinRoomItemClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: RoomItemDto room })
        { _vm.RoomCodeInput = room.RoomCode; _vm.JoinByCodeCommand.Execute(null); }
    }
}
