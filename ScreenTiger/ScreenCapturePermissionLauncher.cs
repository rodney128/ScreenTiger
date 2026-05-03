namespace ScreenTiger;

public sealed partial class ScreenCapturePermissionLauncher
{
    public Task<ScreenCapturePermissionResult> RequestPermissionAsync() =>
        RequestPermissionAsync(CancellationToken.None);

    public partial Task<ScreenCapturePermissionResult> RequestPermissionAsync(CancellationToken cancellationToken);
}

#if !ANDROID
public sealed partial class ScreenCapturePermissionLauncher
{
    public partial Task<ScreenCapturePermissionResult> RequestPermissionAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ScreenCapturePermissionResult.Denied(0, "Screen capture is only available on Android in this phase."));
    }
}
#endif
