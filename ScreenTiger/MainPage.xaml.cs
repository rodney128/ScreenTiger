using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.ApplicationModel;

namespace ScreenTiger;

public partial class MainPage : ContentPage
{
    private readonly ScreenRecordingController _recordingController = new();
    private readonly RecordingFileViewer _recordingFileViewer = new();
    private readonly AiAssistantLauncher _aiAssistantLauncher = new();
    private readonly RecordingPackageBuilder _recordingPackageBuilder = new();
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

    private async void OnViewMp4Clicked(object? sender, EventArgs e)
    {
        string? viewPath = !string.IsNullOrWhiteSpace(_viewState.PublicContentUri)
            ? _viewState.PublicContentUri
            : _viewState.SavedFilePath;

        var openResult = await _recordingFileViewer.OpenSavedMp4Async(viewPath);
        if (!openResult.IsSuccess)
        {
            await DisplayAlertAsync("ScreenTiger", openResult.ErrorMessage ?? "Unable to open the saved MP4.", "OK");
        }
    }

    private async void OnSendToChatGptClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewState.SavedFilePath))
        {
            await DisplayAlertAsync("ScreenTiger", "The saved MP4 could not be found. Record again and try Open ChatGPT.", "OK");
            return;
        }

        if (!File.Exists(_viewState.SavedFilePath))
        {
            await DisplayAlertAsync("ScreenTiger", "The saved MP4 could not be found. Record again and try Open ChatGPT.", "OK");
            return;
        }

        string packageFileName = $"ScreenTiger_Package_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
        const string packageDisplayFolder = "Download/ScreenTiger";

        string reportText = SupportReportBuilder.BuildCompactReport(
            _viewState.SavedFilePath,
            _viewState.SavedDuration,
            _viewState.SavedUsedMicrophone,
            _viewState.PublicDisplayFolder,
            _viewState.PublicContentUri,
            _viewState.ExportWarningMessage,
            "Movies/ScreenTiger",
            packageFileName,
            packageDisplayFolder);

        try
        {
            await Clipboard.SetTextAsync(reportText);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("ScreenTiger", $"Could not copy the AI report to clipboard: {ex.Message}", "OK");
            return;
        }

        var packageResult = await _recordingPackageBuilder.CreatePackageAsync(
            _viewState.SavedFilePath,
            reportText,
            packageFileName,
            CancellationToken.None);

        var sendResult = await _aiAssistantLauncher.OpenChatGptAsync(_viewState.SavedFilePath, reportText, CancellationToken.None);

        string userMessage = sendResult.Outcome switch
        {
            AiAssistantLaunchOutcome.OpenedChatGpt => packageResult.IsSuccess
                ? "ChatGPT opened. The AI report was copied to your clipboard. A recording package ZIP was saved to Download/ScreenTiger."
                : "ChatGPT opened. The AI report was copied to your clipboard, but ScreenTiger could not create the ZIP package.",
            AiAssistantLaunchOutcome.ChatGptNotInstalled => packageResult.IsSuccess
                ? "ChatGPT is not installed. The AI report was copied and the recording package was saved to Download/ScreenTiger."
                : "ChatGPT is not installed. The AI report was copied, but ScreenTiger could not create the ZIP package.",
            _ => packageResult.IsSuccess
                ? "ScreenTiger could not open ChatGPT. The AI report was copied to your clipboard. A recording package ZIP was saved to Download/ScreenTiger."
                : "ScreenTiger could not open ChatGPT. The AI report was copied to your clipboard, but ScreenTiger could not create the ZIP package."
        };

        await DisplayAlertAsync("ScreenTiger", userMessage, "OK");
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
        _viewState.LatestRecordingUsedMicrophone = startResult.IsMicrophoneEnabled;
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
            savedFilePath: null,
            savedDuration: null,
            savedUsedMicrophone: null,
            publicContentUri: null,
            publicDisplayFolder: null,
            exportWarningMessage: null,
            showSendToChatGptButton: false,
            isSendToChatGptButtonEnabled: false,
            showViewMp4Button: false,
            isViewMp4ButtonEnabled: false,
            primaryButtonText: "Start Recording",
            showPrimaryButton: true,
            isPrimaryButtonEnabled: true,
            showDualActions: false,
            isBusyState: false,
            showMicrophoneStatus: false,
            showFooterMicrophoneNote: true,
            microphoneStatusText: string.Empty);
    }

    private void SetPrePermissionState()
    {
        _viewState.SetState(
            RecordingUiState.PrePermission,
            "Android will ask for permission to capture your screen.",
            "Microphone audio is included when permission is granted. Internal app audio is not captured.",
            savedFilePath: null,
            savedDuration: null,
            savedUsedMicrophone: null,
            publicContentUri: null,
            publicDisplayFolder: null,
            exportWarningMessage: null,
            showSendToChatGptButton: false,
            isSendToChatGptButtonEnabled: false,
            showViewMp4Button: false,
            isViewMp4ButtonEnabled: false,
            primaryButtonText: string.Empty,
            showPrimaryButton: false,
            isPrimaryButtonEnabled: false,
            showDualActions: true,
            isBusyState: false,
            showMicrophoneStatus: false,
            showFooterMicrophoneNote: false,
            microphoneStatusText: string.Empty);
    }

    private void SetStartingState()
    {
        _viewState.SetState(
            RecordingUiState.Starting,
            "Preparing Android screen capture...",
            string.Empty,
            savedFilePath: null,
            savedDuration: null,
            savedUsedMicrophone: null,
            publicContentUri: null,
            publicDisplayFolder: null,
            exportWarningMessage: null,
            showSendToChatGptButton: false,
            isSendToChatGptButtonEnabled: false,
            showViewMp4Button: false,
            isViewMp4ButtonEnabled: false,
            primaryButtonText: string.Empty,
            showPrimaryButton: false,
            isPrimaryButtonEnabled: false,
            showDualActions: false,
            isBusyState: true,
            showMicrophoneStatus: false,
            showFooterMicrophoneNote: true,
            microphoneStatusText: string.Empty);
    }

    private void SetRecordingState(bool microphoneEnabled)
    {
        _viewState.SetState(
            RecordingUiState.Recording,
            "Recording in progress",
            "ScreenTiger is recording your screen.",
            savedFilePath: null,
            savedDuration: null,
            savedUsedMicrophone: null,
            publicContentUri: null,
            publicDisplayFolder: null,
            exportWarningMessage: null,
            showSendToChatGptButton: false,
            isSendToChatGptButtonEnabled: false,
            showViewMp4Button: false,
            isViewMp4ButtonEnabled: false,
            primaryButtonText: "Stop Recording",
            showPrimaryButton: true,
            isPrimaryButtonEnabled: true,
            showDualActions: false,
            isBusyState: false,
            showMicrophoneStatus: true,
            showFooterMicrophoneNote: true,
            microphoneStatusText: microphoneEnabled ? "Microphone: On" : "Microphone: Off — video only");
    }

    private void SetStoppingState()
    {
        _viewState.SetState(
            RecordingUiState.Stopping,
            "Stopping recording...",
            "Finalizing video file.",
            savedFilePath: _viewState.SavedFilePath,
            savedDuration: _viewState.SavedDuration,
            savedUsedMicrophone: _viewState.SavedUsedMicrophone,
            publicContentUri: _viewState.PublicContentUri,
            publicDisplayFolder: _viewState.PublicDisplayFolder,
            exportWarningMessage: _viewState.ExportWarningMessage,
            showSendToChatGptButton: false,
            isSendToChatGptButtonEnabled: false,
            showViewMp4Button: false,
            isViewMp4ButtonEnabled: false,
            primaryButtonText: string.Empty,
            showPrimaryButton: false,
            isPrimaryButtonEnabled: false,
            showDualActions: false,
            isBusyState: true,
            showMicrophoneStatus: false,
            showFooterMicrophoneNote: true,
            microphoneStatusText: string.Empty);
    }

    private void SetSavedState(ScreenRecordingStopResult stopResult)
    {
        string resolvedFileName = stopResult.FileName;
        if (string.IsNullOrWhiteSpace(resolvedFileName))
        {
            if (!string.IsNullOrWhiteSpace(stopResult.PrivateFilePath))
            {
                resolvedFileName = Path.GetFileName(stopResult.PrivateFilePath);
            }
            else if (!string.IsNullOrWhiteSpace(stopResult.SavedFilePath) && !stopResult.SavedFilePath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
            {
                resolvedFileName = Path.GetFileName(stopResult.SavedFilePath);
            }
        }

        string fileName = string.IsNullOrWhiteSpace(resolvedFileName)
            ? "Saved file: Unavailable"
            : $"Saved file: {resolvedFileName}";

        string detailText = stopResult.PublicExportSucceeded
            ? string.IsNullOrWhiteSpace(stopResult.PublicDisplayFolder)
                ? fileName
                : $"{fileName}{Environment.NewLine}Saved to {stopResult.PublicDisplayFolder}"
            : string.IsNullOrWhiteSpace(stopResult.PrivateFilePath)
                ? fileName
                : $"{fileName}{Environment.NewLine}Recording saved in app storage. Export to Movies/ScreenTiger failed.";

        bool hasSavedPath = !string.IsNullOrWhiteSpace(stopResult.SavedFilePath)
            || !string.IsNullOrWhiteSpace(stopResult.PrivateFilePath);

        string? exportWarningMessage = stopResult.PublicExportSucceeded
            ? null
            : string.IsNullOrWhiteSpace(stopResult.PrivateFilePath)
                ? "Recording stopped, but the MP4 file could not be found."
                : "Recording saved in app storage. Export to Movies/ScreenTiger failed.";

        _viewState.SetState(
            RecordingUiState.Saved,
            "Recording saved",
            detailText,
            savedFilePath: stopResult.PrivateFilePath,
            savedDuration: stopResult.Duration,
            savedUsedMicrophone: _viewState.LatestRecordingUsedMicrophone,
            publicContentUri: stopResult.PublicContentUri,
            publicDisplayFolder: stopResult.PublicDisplayFolder,
            exportWarningMessage: exportWarningMessage,
            showSendToChatGptButton: hasSavedPath,
            isSendToChatGptButtonEnabled: hasSavedPath,
            showViewMp4Button: hasSavedPath,
            isViewMp4ButtonEnabled: hasSavedPath,
            primaryButtonText: "Start Recording",
            showPrimaryButton: true,
            isPrimaryButtonEnabled: true,
            showDualActions: false,
            isBusyState: false,
            showMicrophoneStatus: false,
            showFooterMicrophoneNote: true,
            microphoneStatusText: string.Empty);
    }

    private sealed class MainPageViewState : INotifyPropertyChanged
    {
        private RecordingUiState _currentState;
        private string _titleText = string.Empty;
        private string _detailText = string.Empty;
        private string? _savedFilePath;
        private TimeSpan? _savedDuration;
        private bool? _savedUsedMicrophone;
        private string? _publicContentUri;
        private string? _publicDisplayFolder;
        private string? _exportWarningMessage;
        private bool? _latestRecordingUsedMicrophone;
        private bool _showSendToChatGptButton;
        private bool _isSendToChatGptButtonEnabled;
        private bool _showViewMp4Button;
        private bool _isViewMp4ButtonEnabled;
        private string _primaryButtonText = string.Empty;
        private bool _showPrimaryButton;
        private bool _isPrimaryButtonEnabled;
        private bool _showDualActionButtons;
        private bool _isBusyState;
        private bool _showMicrophoneStatus;
        private bool _showFooterMicrophoneNote = true;
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

        public bool ShowSendToChatGptButton
        {
            get => _showSendToChatGptButton;
            private set => SetProperty(ref _showSendToChatGptButton, value);
        }

        public bool IsSendToChatGptButtonEnabled
        {
            get => _isSendToChatGptButtonEnabled;
            private set => SetProperty(ref _isSendToChatGptButtonEnabled, value);
        }

        public string? SavedFilePath
        {
            get => _savedFilePath;
            private set => SetProperty(ref _savedFilePath, value);
        }

        public TimeSpan? SavedDuration
        {
            get => _savedDuration;
            private set => SetProperty(ref _savedDuration, value);
        }

        public bool? SavedUsedMicrophone
        {
            get => _savedUsedMicrophone;
            private set => SetProperty(ref _savedUsedMicrophone, value);
        }

        public string? ExportWarningMessage
        {
            get => _exportWarningMessage;
            private set => SetProperty(ref _exportWarningMessage, value);
        }

        public string? PublicContentUri
        {
            get => _publicContentUri;
            private set => SetProperty(ref _publicContentUri, value);
        }

        public string? PublicDisplayFolder
        {
            get => _publicDisplayFolder;
            private set => SetProperty(ref _publicDisplayFolder, value);
        }

        public bool? LatestRecordingUsedMicrophone
        {
            get => _latestRecordingUsedMicrophone;
            set => SetProperty(ref _latestRecordingUsedMicrophone, value);
        }

        public bool ShowViewMp4Button
        {
            get => _showViewMp4Button;
            private set => SetProperty(ref _showViewMp4Button, value);
        }

        public bool IsViewMp4ButtonEnabled
        {
            get => _isViewMp4ButtonEnabled;
            private set => SetProperty(ref _isViewMp4ButtonEnabled, value);
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

        public bool ShowFooterMicrophoneNote
        {
            get => _showFooterMicrophoneNote;
            private set => SetProperty(ref _showFooterMicrophoneNote, value);
        }

        public void SetState(
            RecordingUiState state,
            string title,
            string detail,
            string? savedFilePath,
            TimeSpan? savedDuration,
            bool? savedUsedMicrophone,
            string? publicContentUri,
            string? publicDisplayFolder,
            string? exportWarningMessage,
            bool showSendToChatGptButton,
            bool isSendToChatGptButtonEnabled,
            bool showViewMp4Button,
            bool isViewMp4ButtonEnabled,
            string primaryButtonText,
            bool showPrimaryButton,
            bool isPrimaryButtonEnabled,
            bool showDualActions,
            bool isBusyState,
            bool showMicrophoneStatus,
            bool showFooterMicrophoneNote,
            string microphoneStatusText)
        {
            CurrentState = state;
            TitleText = title;
            DetailText = detail;
            SavedFilePath = savedFilePath;
            SavedDuration = savedDuration;
            SavedUsedMicrophone = savedUsedMicrophone;
            PublicContentUri = publicContentUri;
            PublicDisplayFolder = publicDisplayFolder;
            ExportWarningMessage = exportWarningMessage;
            ShowSendToChatGptButton = showSendToChatGptButton;
            IsSendToChatGptButtonEnabled = isSendToChatGptButtonEnabled;
            ShowViewMp4Button = showViewMp4Button;
            IsViewMp4ButtonEnabled = isViewMp4ButtonEnabled;
            PrimaryButtonText = primaryButtonText;
            ShowPrimaryButton = showPrimaryButton;
            IsPrimaryButtonEnabled = isPrimaryButtonEnabled;
            ShowDualActionButtons = showDualActions;
            IsBusyState = isBusyState;
            ShowMicrophoneStatus = showMicrophoneStatus;
            ShowFooterMicrophoneNote = showFooterMicrophoneNote;
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

