namespace ScreenTiger;

public sealed record FileOpenResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static FileOpenResult Success() => new(true);

    public static FileOpenResult Failure(string message) => new(false, message);
}

public sealed partial class RecordingFileViewer
{
    public Task<FileOpenResult> OpenSavedMp4Async(string? filePath) =>
        OpenSavedMp4Async(filePath, CancellationToken.None);

    public partial Task<FileOpenResult> OpenSavedMp4Async(string? filePath, CancellationToken cancellationToken);
}

#if !ANDROID
public sealed partial class RecordingFileViewer
{
    public partial Task<FileOpenResult> OpenSavedMp4Async(string? filePath, CancellationToken cancellationToken)
    {
        return Task.FromResult(FileOpenResult.Failure("No video player could open this MP4 on this device."));
    }
}
#endif
