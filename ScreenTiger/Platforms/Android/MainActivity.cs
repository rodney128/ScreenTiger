using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Activity.Result;
using Microsoft.Maui.ApplicationModel;

namespace ScreenTiger;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int ScreenCaptureRequestCode = 5107;
    private static TaskCompletionSource<ScreenCapturePermissionResult>? _screenCaptureRequest;

    internal static Task<ScreenCapturePermissionResult> RequestScreenCapturePermissionAsync(Intent permissionIntent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(permissionIntent);

        var activity = Platform.CurrentActivity as MainActivity;
        if (activity is null)
        {
            throw new InvalidOperationException("No active Android activity is available to launch screen capture permission.");
        }

        if (_screenCaptureRequest is not null)
        {
            throw new InvalidOperationException("A screen capture permission request is already in progress.");
        }

        _screenCaptureRequest = new TaskCompletionSource<ScreenCapturePermissionResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                _screenCaptureRequest?.TrySetResult(ScreenCapturePermissionResult.Denied((int)Result.Canceled, "Screen capture permission request canceled."));
            });
        }

        activity.RunOnUiThread(() => activity.StartActivityForResult(permissionIntent, ScreenCaptureRequestCode));
        return _screenCaptureRequest.Task;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode != ScreenCaptureRequestCode || _screenCaptureRequest is null)
        {
            return;
        }

        var permissionResult = resultCode == Result.Ok
            ? ScreenCapturePermissionResult.Granted((int)resultCode, data)
            : ScreenCapturePermissionResult.Denied((int)resultCode, "Screen capture permission was denied or canceled.");

        _screenCaptureRequest.TrySetResult(permissionResult);
        _screenCaptureRequest = null;
    }
}
