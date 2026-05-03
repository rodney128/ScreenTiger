using Android.Content;
using Android.Media.Projection;
using Microsoft.Maui.ApplicationModel;

namespace ScreenTiger;

public sealed partial class ScreenCapturePermissionLauncher
{
    public partial async Task<ScreenCapturePermissionResult> RequestPermissionAsync(CancellationToken cancellationToken = default)
    {
        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            return ScreenCapturePermissionResult.Denied(0, "Android activity is unavailable.");
        }

        var manager = activity.GetSystemService(Android.Content.Context.MediaProjectionService) as MediaProjectionManager;
        if (manager is null)
        {
            return ScreenCapturePermissionResult.Denied(0, "MediaProjection service is unavailable.");
        }

        var permissionIntent = manager.CreateScreenCaptureIntent();
        return await MainActivity.RequestScreenCapturePermissionAsync(permissionIntent, cancellationToken).ConfigureAwait(false);
    }
}
