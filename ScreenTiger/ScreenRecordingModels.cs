namespace ScreenTiger;

public enum RecordingUiState
{
    Idle,
    PrePermission,
    Starting,
    Recording,
    Stopping,
    Saved
}

public sealed record ScreenCapturePermissionResult(bool IsGranted, int ResultCode, object? PermissionData, string? ErrorMessage = null)
{
    public static ScreenCapturePermissionResult Granted(int resultCode, object? permissionData) =>
        new(true, resultCode, permissionData);

    public static ScreenCapturePermissionResult Denied(int resultCode, string? errorMessage = null) =>
        new(false, resultCode, null, errorMessage);
}

public sealed record ScreenRecordingStartResult(bool IsSuccess, bool IsMicrophoneEnabled, string? ErrorMessage = null)
{
    public static ScreenRecordingStartResult Success(bool isMicrophoneEnabled) =>
        new(true, isMicrophoneEnabled);

    public static ScreenRecordingStartResult Failure(string errorMessage) =>
        new(false, false, errorMessage);
}

public sealed record ScreenRecordingStopResult(
    bool IsSuccess,
    string? SavedFilePath,
    TimeSpan Duration,
    string? ErrorMessage = null,
    string? PrivateFilePath = null,
    string? PublicContentUri = null,
    string? PublicDisplayFolder = null,
    string? FileName = null,
    bool PublicExportSucceeded = false,
    string? PublicExportErrorMessage = null)
{
    public static ScreenRecordingStopResult Success(
        string savedFilePath,
        TimeSpan duration,
        string? privateFilePath,
        string? publicContentUri,
        string? publicDisplayFolder,
        string? fileName,
        bool publicExportSucceeded,
        string? publicExportErrorMessage) =>
        new(
            true,
            savedFilePath,
            duration,
            null,
            privateFilePath,
            publicContentUri,
            publicDisplayFolder,
            fileName,
            publicExportSucceeded,
            publicExportErrorMessage);

    public static ScreenRecordingStopResult Failure(string errorMessage) =>
        new(false, null, TimeSpan.Zero, errorMessage);
}

public sealed record ScreenRecordingStatus(bool IsRecording, bool IsMicrophoneEnabled, string? SavedFilePath, TimeSpan Duration);
