namespace ScreenTiger
{
    public partial class MainPage : ContentPage
    {
        private RecordingState _currentState;

        public MainPage()
        {
            InitializeComponent();
            SetRecordingState(RecordingState.Idle);
        }

        private async void OnPrimaryButtonClicked(object? sender, EventArgs e)
        {
            switch (_currentState)
            {
                case RecordingState.Idle:
                    SetRecordingState(RecordingState.PrePermission);
                    break;
                case RecordingState.PrePermission:
                    await ContinueFromPrePermissionAsync();
                    break;
                case RecordingState.Recording:
                    SetRecordingState(RecordingState.Stopping);
                    StatusLabel.Text = "Stop flow will be implemented in Phase 2.";
                    SetRecordingState(RecordingState.Idle);
                    break;
                case RecordingState.Saved:
                    StatusLabel.Text = "ChatGPT sharing will be enabled in a later phase.";
                    break;
            }
        }

        private void OnSecondaryButtonClicked(object? sender, EventArgs e)
        {
            if (_currentState == RecordingState.PrePermission)
            {
                SetRecordingState(RecordingState.Idle);
                StatusLabel.Text = "Screen capture setup cancelled.";
            }
        }

        private async Task ContinueFromPrePermissionAsync()
        {
            SetRecordingState(RecordingState.Starting);
            await Task.Delay(450);
            SetRecordingState(RecordingState.Idle);
            StatusLabel.Text = "Recording engine will be implemented in Phase 2.";
        }

        private void SetRecordingState(RecordingState state)
        {
            _currentState = state;

            StateActivityIndicator.IsVisible = state == RecordingState.Starting || state == RecordingState.Stopping;
            StateActivityIndicator.IsRunning = StateActivityIndicator.IsVisible;

            SecondaryButton.IsVisible = false;
            SecondaryButton.IsEnabled = true;
            PrimaryButton.IsEnabled = true;

            switch (state)
            {
                case RecordingState.Idle:
                    PageTitleLabel.Text = "ScreenTiger";
                    SubtitleLabel.Text = "Record your screen and package the result for AI review.";
                    BodyLabel.Text = string.Empty;
                    PrimaryButton.Text = "Start Recording";
                    MicrophoneNoteLabel.IsVisible = true;
                    break;

                case RecordingState.PrePermission:
                    PageTitleLabel.Text = "ScreenTiger";
                    SubtitleLabel.Text = "Android will ask for permission to capture your screen.";
                    BodyLabel.Text = "Microphone audio is included when permission is granted. Internal app audio is not captured.";
                    PrimaryButton.Text = "Continue";
                    SecondaryButton.IsVisible = true;
                    SecondaryButton.Text = "Cancel";
                    MicrophoneNoteLabel.IsVisible = false;
                    break;

                case RecordingState.Starting:
                    PageTitleLabel.Text = "ScreenTiger";
                    SubtitleLabel.Text = "Preparing Android screen capture...";
                    BodyLabel.Text = string.Empty;
                    PrimaryButton.Text = "Continue";
                    PrimaryButton.IsEnabled = false;
                    SecondaryButton.IsVisible = true;
                    SecondaryButton.Text = "Cancel";
                    SecondaryButton.IsEnabled = false;
                    MicrophoneNoteLabel.IsVisible = false;
                    break;

                case RecordingState.Recording:
                    PageTitleLabel.Text = "ScreenTiger";
                    SubtitleLabel.Text = "Recording";
                    BodyLabel.Text = "Live recording controls will be enabled in Phase 2.";
                    PrimaryButton.Text = "Stop Recording";
                    MicrophoneNoteLabel.IsVisible = false;
                    break;

                case RecordingState.Stopping:
                    PageTitleLabel.Text = "ScreenTiger";
                    SubtitleLabel.Text = "Stopping";
                    BodyLabel.Text = "Finalizing recording flow will be enabled in Phase 2.";
                    PrimaryButton.Text = "Stop Recording";
                    PrimaryButton.IsEnabled = false;
                    MicrophoneNoteLabel.IsVisible = false;
                    break;

                case RecordingState.Saved:
                    PageTitleLabel.Text = "ScreenTiger";
                    SubtitleLabel.Text = "Recording saved";
                    BodyLabel.Text = "Saved-state sharing workflow will be enabled in later phases.";
                    PrimaryButton.Text = "ChatGPT";
                    PrimaryButton.IsEnabled = false;
                    MicrophoneNoteLabel.IsVisible = false;
                    break;
            }
        }

        private enum RecordingState
        {
            Idle,
            PrePermission,
            Starting,
            Recording,
            Stopping,
            Saved
        }
    }
}
