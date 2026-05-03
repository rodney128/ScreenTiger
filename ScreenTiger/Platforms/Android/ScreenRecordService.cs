using Android.App;
using Android.Content;
using Android.Hardware.Display;
using Android.Media;
using Android.Media.Projection;
using Android.OS;
using Android.Runtime;
using Java.IO;
using Application = Android.App.Application;
using Environment = Android.OS.Environment;

namespace ScreenTiger;

[Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMediaProjection, Exported = false)]
public sealed class ScreenRecordService : Service
{
    private const int VirtualDisplayFlagAutoMirror = 16;
    private const string NotificationChannelId = "screen_record_channel";
    private const int NotificationId = 4226;

    private const string ActionStart = "ScreenTiger.ScreenRecordService.START";
    private const string ActionStop = "ScreenTiger.ScreenRecordService.STOP";

    private const string ExtraRequestId = "request_id";
    private const string ExtraPermissionResultCode = "permission_result_code";
    private const string ExtraPermissionData = "permission_data";
    private const string ExtraMicrophoneEnabled = "microphone_enabled";

    private readonly object _sync = new();

    private bool _isRecording;
    private bool _isFinalizing;
    private bool _microphoneEnabled;
    private string? _activeSessionRequestId;
    private DateTimeOffset _startedAtUtc;
    private string? _savedFilePath;

    private MediaProjection? _mediaProjection;
    private MediaProjection.Callback? _mediaProjectionCallback;
    private Handler? _mediaProjectionCallbackHandler;
    private MediaRecorder? _mediaRecorder;
    private VirtualDisplay? _virtualDisplay;

    public static void StartRecording(Context context, string requestId, int permissionResultCode, Intent permissionData, bool enableMicrophone)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentNullException.ThrowIfNull(permissionData);

        var intent = new Intent(context, typeof(ScreenRecordService));
        intent.SetAction(ActionStart);
        intent.PutExtra(ExtraRequestId, requestId);
        intent.PutExtra(ExtraPermissionResultCode, permissionResultCode);
        intent.PutExtra(ExtraPermissionData, permissionData);
        intent.PutExtra(ExtraMicrophoneEnabled, enableMicrophone);
        context.StartForegroundService(intent);
    }

    public static void StopRecording(Context context, string requestId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestId);

        var intent = new Intent(context, typeof(ScreenRecordService));
        intent.SetAction(ActionStop);
        intent.PutExtra(ExtraRequestId, requestId);
        context.StartService(intent);
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent is null)
        {
            return StartCommandResult.NotSticky;
        }

        var action = intent.Action;
        if (string.Equals(action, ActionStart, StringComparison.Ordinal))
        {
            HandleStart(intent);
            return StartCommandResult.Sticky;
        }

        if (string.Equals(action, ActionStop, StringComparison.Ordinal))
        {
            HandleStop(intent);
            return StartCommandResult.NotSticky;
        }

        return StartCommandResult.NotSticky;
    }

    private void HandleStart(Intent intent)
    {
        lock (_sync)
        {
            if (_isRecording)
            {
                var duplicateRequestId = intent.GetStringExtra(ExtraRequestId) ?? string.Empty;
                ScreenRecordingController.NotifyStartFailed(duplicateRequestId, "Recording is already in progress.");
                return;
            }

            var requestId = intent.GetStringExtra(ExtraRequestId) ?? string.Empty;
            var permissionResultCode = intent.GetIntExtra(ExtraPermissionResultCode, (int)Result.Canceled);
            var permissionData = intent.GetParcelableExtra(ExtraPermissionData) as Intent;
            var enableMicrophone = intent.GetBooleanExtra(ExtraMicrophoneEnabled, false);

            if (string.IsNullOrWhiteSpace(requestId) || permissionData is null)
            {
                ScreenRecordingController.NotifyStartFailed(requestId, "Missing screen capture permission data.");
                return;
            }

            try
            {
                EnsureNotificationChannel();
                StartForeground(NotificationId, BuildNotification());

                StartProjection(permissionResultCode, permissionData, enableMicrophone);

                _isRecording = true;
                _isFinalizing = false;
                _microphoneEnabled = enableMicrophone;
                _activeSessionRequestId = requestId;
                _startedAtUtc = DateTimeOffset.UtcNow;

                ScreenRecordingController.NotifyStartSucceeded(requestId, _microphoneEnabled);
            }
            catch (Exception ex)
            {
                CleanupResources();
                StopForeground(StopForegroundFlags.Remove);
                ScreenRecordingController.NotifyStartFailed(requestId, $"Unable to start recording: {ex.Message}");
                StopSelf();
            }
        }
    }

    private void HandleStop(Intent intent)
    {
        string requestId = intent.GetStringExtra(ExtraRequestId) ?? string.Empty;
        FinalizeRecording(requestId, notifyController: true);
    }

    private void HandleProjectionStoppedBySystem()
    {
        string requestId;
        lock (_sync)
        {
            requestId = _activeSessionRequestId ?? string.Empty;
        }

        FinalizeRecording(requestId, notifyController: false);
    }

    private void FinalizeRecording(string requestId, bool notifyController)
    {
        lock (_sync)
        {
            if (!_isRecording)
            {
                if (notifyController)
                {
                    ScreenRecordingController.NotifyStopFailed(requestId, "Recording is not currently running.");
                }

                return;
            }

            if (_isFinalizing)
            {
                if (notifyController)
                {
                    ScreenRecordingController.NotifyStopFailed(requestId, "Recording finalization is already in progress.");
                }

                return;
            }

            _isFinalizing = true;
        }

        try
        {
            _mediaRecorder?.Stop();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MediaRecorder stop failed: {ex.Message}");
        }

        var duration = DateTimeOffset.UtcNow - _startedAtUtc;
        CleanupResources();

        lock (_sync)
        {
            _isRecording = false;
            _isFinalizing = false;
            _activeSessionRequestId = null;
            _microphoneEnabled = false;
            _startedAtUtc = DateTimeOffset.MinValue;
        }

        StopForegroundCompat();

        if (notifyController)
        {
            ScreenRecordingController.NotifyStopCompleted(requestId, _savedFilePath, duration);
        }

        StopSelf();
    }

    private void StartProjection(int permissionResultCode, Intent permissionData, bool enableMicrophone)
    {
        var manager = GetSystemService(MediaProjectionService) as MediaProjectionManager;
        if (manager is null)
        {
            throw new InvalidOperationException("MediaProjectionManager is unavailable.");
        }

        _mediaProjection = manager.GetMediaProjection(permissionResultCode, permissionData);
        if (_mediaProjection is null)
        {
            throw new InvalidOperationException("MediaProjection permission was not granted by Android.");
        }

        _mediaProjectionCallbackHandler = new Handler(Looper.MainLooper);
        _mediaProjectionCallback = new ScreenMediaProjectionCallback(this);
        _mediaProjection.RegisterCallback(_mediaProjectionCallback, _mediaProjectionCallbackHandler);

        var metrics = Resources?.DisplayMetrics;
        if (metrics is null)
        {
            throw new InvalidOperationException("Unable to get display metrics for screen recording.");
        }

        var outputFilePath = CreateOutputFilePath();

        _mediaRecorder = new MediaRecorder(this);
        _mediaRecorder.SetVideoSource(VideoSource.Surface);

        if (enableMicrophone)
        {
            _mediaRecorder.SetAudioSource(AudioSource.Mic);
        }

        _mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
        _mediaRecorder.SetOutputFile(outputFilePath);
        _mediaRecorder.SetVideoEncoder(VideoEncoder.H264);
        _mediaRecorder.SetVideoSize(metrics.WidthPixels, metrics.HeightPixels);
        _mediaRecorder.SetVideoFrameRate(30);
        _mediaRecorder.SetVideoEncodingBitRate(8 * 1024 * 1024);

        if (enableMicrophone)
        {
            _mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
            _mediaRecorder.SetAudioChannels(1);
            _mediaRecorder.SetAudioEncodingBitRate(128000);
            _mediaRecorder.SetAudioSamplingRate(44100);
        }

        _mediaRecorder.Prepare();

        _virtualDisplay = _mediaProjection.CreateVirtualDisplay(
            "ScreenTigerCapture",
            metrics.WidthPixels,
            metrics.HeightPixels,
            (int)metrics.DensityDpi,
            (Android.Views.DisplayFlags)VirtualDisplayFlagAutoMirror,
            _mediaRecorder.Surface,
            null,
            null);

        if (_virtualDisplay is null)
        {
            throw new InvalidOperationException("Unable to create virtual display for recording.");
        }

        _savedFilePath = outputFilePath;
        _mediaRecorder.Start();
    }

    private void StopForegroundCompat()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
        {
            StopForeground(StopForegroundFlags.Remove);
        }
        else
        {
            StopForeground(true);
        }
    }

    private string CreateOutputFilePath()
    {
        var baseFolder = GetExternalFilesDir(Environment.DirectoryMovies)
            ?? throw new InvalidOperationException("Unable to resolve app-private Movies directory.");

        var screenTigerFolder = new Java.IO.File(baseFolder, "ScreenTiger");
        if (!screenTigerFolder.Exists() && !screenTigerFolder.Mkdirs())
        {
            throw new InvalidOperationException("Unable to create ScreenTiger recording folder.");
        }

        var fileName = $"ScreenTiger_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mp4";
        return System.IO.Path.Combine(screenTigerFolder.AbsolutePath ?? string.Empty, fileName);
    }

    private Notification BuildNotification()
    {
        var builder = new Notification.Builder(this, NotificationChannelId)
            .SetContentTitle("ScreenTiger recording")
            .SetContentText("ScreenTiger is recording your screen.")
            .SetSmallIcon(Resource.Mipmap.tigericon)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true);

        return builder.Build();
    }

    private void EnsureNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var manager = GetSystemService(NotificationService) as NotificationManager;
        if (manager is null)
        {
            return;
        }

        if (manager.GetNotificationChannel(NotificationChannelId) is not null)
        {
            return;
        }

        var channel = new NotificationChannel(
            NotificationChannelId,
            "Screen Recording",
            NotificationImportance.Low)
        {
            Description = "Shows recording status while ScreenTiger captures your screen."
        };

        manager.CreateNotificationChannel(channel);
    }

    private void CleanupResources()
    {
        try
        {
            _virtualDisplay?.Release();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"VirtualDisplay release failed: {ex.Message}");
        }

        _virtualDisplay?.Dispose();
        _virtualDisplay = null;

        try
        {
            _mediaRecorder?.Reset();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MediaRecorder reset failed: {ex.Message}");
        }

        _mediaRecorder?.Release();
        _mediaRecorder?.Dispose();
        _mediaRecorder = null;

        if (_mediaProjection is not null && _mediaProjectionCallback is not null)
        {
            try
            {
                _mediaProjection.UnregisterCallback(_mediaProjectionCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MediaProjection callback unregister failed: {ex.Message}");
            }
        }

        _mediaProjectionCallback = null;
        _mediaProjectionCallbackHandler = null;

        try
        {
            _mediaProjection?.Stop();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MediaProjection stop failed: {ex.Message}");
        }

        _mediaProjection?.Dispose();
        _mediaProjection = null;
    }

    private sealed class ScreenMediaProjectionCallback : MediaProjection.Callback
    {
        private readonly WeakReference<ScreenRecordService> _serviceReference;

        public ScreenMediaProjectionCallback(ScreenRecordService service)
        {
            _serviceReference = new WeakReference<ScreenRecordService>(service);
        }

        public override void OnStop()
        {
            if (_serviceReference.TryGetTarget(out var service))
            {
                service.HandleProjectionStoppedBySystem();
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CleanupResources();
        }

        base.Dispose(disposing);
    }
}
