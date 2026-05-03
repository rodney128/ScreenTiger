namespace ScreenTiger;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnStartRecordingClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("ScreenTiger", "Screen recording will be added in Phase 2.", "OK");
    }
}

