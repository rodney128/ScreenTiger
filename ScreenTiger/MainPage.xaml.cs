namespace ScreenTiger
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OnStartRecordingClicked(object? sender, EventArgs e)
        {
            StatusLabel.Text = "Recording engine will be added in Phase 2.";
        }
    }
}
