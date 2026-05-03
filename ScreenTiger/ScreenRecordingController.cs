namespace ScreenTiger;

public sealed partial class ScreenRecordingController
{
    public event EventHandler<ScreenRecordingStopResult>? RecordingStopped;

    public string? LatestSavedFilePath { get; private set; }

    private void RaiseRecordingStopped(ScreenRecordingStopResult result)
    {
        if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.SavedFilePath))
        {
            LatestSavedFilePath = result.SavedFilePath;
        }

        RecordingStopped?.Invoke(this, result);
    }

    public Task<ScreenRecordingStartResult> StartAsync(bool enableMicrophone) =>
        StartAsync(enableMicrophone, CancellationToken.None);

    public partial Task<ScreenRecordingStartResult> StartAsync(bool enableMicrophone, CancellationToken cancellationToken);

    public Task<ScreenRecordingStopResult> StopAsync() =>
        StopAsync(CancellationToken.None);

    public partial Task<ScreenRecordingStopResult> StopAsync(CancellationToken cancellationToken);
}

#if !ANDROID
public sealed partial class ScreenRecordingController
{
    public partial Task<ScreenRecordingStartResult> StartAsync(bool enableMicrophone, CancellationToken cancellationToken)
    {
        return Task.FromResult(ScreenRecordingStartResult.Failure("Screen recording is only available on Android in this phase."));
    }

    public partial Task<ScreenRecordingStopResult> StopAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ScreenRecordingStopResult.Failure("Screen recording is only available on Android in this phase."));
    }
}
#endif
