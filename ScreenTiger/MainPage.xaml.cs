using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.ApplicationModel;

namespace ScreenTiger;

public partial class MainPage : ContentPage
{
    private readonly ScreenRecordingController _recordingController = new();
    private readonly MainPageViewState _viewState = new();

    public MainPage()
    {
        InitializeComponent();
        BindingContext = _viewState;
        _recordingController.RecordingStopped += OnRecordingStopped;
        SetIdleState();
    }

    private async void OnPrimaryActionClicked(object? sender, EventArgs e)
    {
        if (_viewState.CurrentState == RecordingUiState.Idle || _viewState.CurrentState == RecordingUiState.Saved)
        {
            SetPrePermissionState();
            return;
        }

        if (_viewState.CurrentState == RecordingUiState.Recording)
        {
            await StopRecordingAsync();
        }
    }

    private async void OnContinueClicked(object? sender, EventArgs e)
    {
        await StartRecordingAsync();
    }

    private async void OnCancelSetupClicked(object? sender, EventArgs e)
    {
        SetIdleState();
        await DisplayAlertAsync("ScreenTiger", "Screen capture setup cancelled.", "OK");
    }

    private async Task StartRecordingAsync()
    {
        SetStartingState();

        bool microphoneEnabled = await RequestMicrophonePermissionAsync();
        var startResult = await _recordingController.StartAsync(microphoneEnabled);
        if (!startResult.IsSuccess)
        {
            SetIdleState();
            await DisplayAlertAsync("ScreenTiger", startResult.ErrorMessage ?? "Unable to start screen recording.", "OK");
            return;
        }

        SetRecordingState(startResult.IsMicrophoneEnabled);
    }

    private async Task StopRecordingAsync()
    {
        SetStoppingState();
        var stopResult = await _recordingController.StopAsync();
        if (!stopResult.IsSuccess)
        {
            SetIdleState();
            await DisplayAlertAsync("ScreenTiger", stopResult.ErrorMessage ?? "Unable to stop recording.", "OK");
            return;
        }

        SetSavedState(stopResult);
    }

    private static async Task<bool> RequestMicrophonePermissionAsync()
    {
        var permissionStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (permissionStatus == PermissionStatus.Granted)
        {
            return true;
        }

        permissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();
        return permissionStatus == PermissionStatus.Granted;
    }

    private void OnRecordingStopped(object? sender, ScreenRecordingStopResult result)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!result.IsSuccess)
            {
                return;
            }

            if (_viewState.CurrentState == RecordingUiState.Stopping)
            {
                SetSavedState(result);
            }
        });
    }

    private void SetIdleState()
    {
        _viewState.SetState(
            RecordingUiState.Idle,
            "Record your screen and package the result for AI review.",
            string.Empty,
            "Start Recording",
            showPrimaryButton: true,
            isPrimaryButtonEnabled: true,
            showDualActions: false,
            isBusyState: false,
            showMicrophoneStatus: false,
            microphoneStatusText: string.Empty);
    }

    private void SetPrePermissionState()
    {
        _viewState.SetState(
            RecordingUiState.PrePermission,
            "Android will ask for permission to capture your screen.",
            "Microphone audio is included when permission is granted. Internal app audio is not captured.",
            string.Empty,
            showPrimaryButton: false,
            isPrimaryButtonEnabled: false,
            showDualActions: true,
            isBusyState: false,
            showMicrophoneStatus: false,
            microphoneStatusText: string.Empty);
    }

    private void SetStartingState()
    {
        _viewState.SetState(
            RecordingUiState.Starting,
            "Preparing Android screen capture...",
            string.Empty,
            string.Empty,
            showPrimaryButton: false,
            isPrimaryButtonEnabled: false,
            showDualActions: false,
            isBusyState: true,
            showMicrophoneStatus: false,
            microphoneStatusText: string.Empty);
    }

    private void SetRecordingState(bool microphoneEnabled)
    {
        _viewState.SetState(
            RecordingUiState.Recording,
            "Recording in progress",
            "ScreenTiger is recording your screen.",
            "Stop Recording",
            showPrimaryButton: true,
            isPrimaryButtonEnabled: true,
            showDualActions: false,
            isBusyState: false,
            showMicrophoneStatus: true,
            microphoneStatusText: microphoneEnabled ? "Microphone: On" : "Microphone: Off — video only");
    }

    private void SetStoppingState()
    {
        _viewState.SetState(
            RecordingUiState.Stopping,
            "Stopping recording...",
            "Finalizing video file.",
            string.Empty,
            showPrimaryButton: false,
            isPrimaryButtonEnabled: false,
            showDualActions: false,
            isBusyState: true,
            showMicrophoneStatus: false,
            microphoneStatusText: string.Empty);
    }

    private void SetSavedState(ScreenRecordingStopResult stopResult)
    {
        string fileName = stopResult.SavedFilePath is null
            ? "Saved to Movies/ScreenTiger"
            : $"Saved file: {Path.GetFileName(stopResult.SavedFilePath)}";

        _viewState.SetState(
            RecordingUiState.Saved,
            "Recording saved",
            fileName,
            "Start Recording",
            showPrimaryButton: true,
            isPrimaryButtonEnabled: true,
            showDualActions: false,
            isBusyState: false,
            showMicrophoneStatus: false,
            microphoneStatusText: string.Empty);
    }

    private sealed class MainPageViewState : INotifyPropertyChanged
    {
        private RecordingUiState _currentState;
        private string _titleText = string.Empty;
        private string _detailText = string.Empty;
        private string _primaryButtonText = string.Empty;
        private bool _showPrimaryButton;
        private bool _isPrimaryButtonEnabled;
        private bool _showDualActionButtons;
        private bool _isBusyState;
        private bool _showMicrophoneStatus;
        private string _microphoneStatusText = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public RecordingUiState CurrentState
        {
            get => _currentState;
            private set => SetProperty(ref _currentState, value);
        }

        public string TitleText
        {
            get => _titleText;
            private set => SetProperty(ref _titleText, value);
        }

        public string DetailText
        {
            get => _detailText;
            private set
            {
                if (SetProperty(ref _detailText, value))
                {
                    OnPropertyChanged(nameof(HasDetailText));
                }
            }
        }

        public bool HasDetailText => !string.IsNullOrWhiteSpace(DetailText);

        public string PrimaryButtonText
        {
            get => _primaryButtonText;
            private set => SetProperty(ref _primaryButtonText, value);
        }

        public bool ShowPrimaryButton
        {
            get => _showPrimaryButton;
            private set => SetProperty(ref _showPrimaryButton, value);
        }

        public bool IsPrimaryButtonEnabled
        {
            get => _isPrimaryButtonEnabled;
            private set => SetProperty(ref _isPrimaryButtonEnabled, value);
        }

        public bool ShowDualActionButtons
        {
            get => _showDualActionButtons;
            private set => SetProperty(ref _showDualActionButtons, value);
        }

        public bool IsBusyState
        {
            get => _isBusyState;
            private set => SetProperty(ref _isBusyState, value);
        }

        public bool ShowMicrophoneStatus
        {
            get => _showMicrophoneStatus;
            private set => SetProperty(ref _showMicrophoneStatus, value);
        }

        public string MicrophoneStatusText
        {
            get => _microphoneStatusText;
            private set => SetProperty(ref _microphoneStatusText, value);
        }

        public void SetState(
            RecordingUiState state,
            string title,
            string detail,
            string primaryButtonText,
            bool showPrimaryButton,
            bool isPrimaryButtonEnabled,
            bool showDualActions,
            bool isBusyState,
            bool showMicrophoneStatus,
            string microphoneStatusText)
        {
            CurrentState = state;
            TitleText = title;
            DetailText = detail;
            PrimaryButtonText = primaryButtonText;
            ShowPrimaryButton = showPrimaryButton;
            IsPrimaryButtonEnabled = isPrimaryButtonEnabled;
            ShowDualActionButtons = showDualActions;
            IsBusyState = isBusyState;
            ShowMicrophoneStatus = showMicrophoneStatus;
            MicrophoneStatusText = microphoneStatusText;
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

