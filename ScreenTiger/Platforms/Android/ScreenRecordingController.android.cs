using Android.App;
using Android.Content;
using Microsoft.Maui.ApplicationModel;
using System.Collections.Concurrent;

namespace ScreenTiger;

public sealed partial class ScreenRecordingController
{
    private static readonly SemaphoreSlim StartStopLock = new(1, 1);
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<ScreenRecordingStartResult>> PendingStarts = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<ScreenRecordingStopResult>> PendingStops = new(StringComparer.Ordinal);

    private bool _isRecording;
    private bool _isStopRequested;
    private DateTimeOffset? _recordingStartedAt;

    public partial async Task<ScreenRecordingStartResult> StartAsync(bool enableMicrophone, CancellationToken cancellationToken = default)
    {
        await StartStopLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_isRecording)
            {
                return ScreenRecordingStartResult.Failure("Recording is already in progress.");
            }

            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return ScreenRecordingStartResult.Failure("Android activity is unavailable.");
            }

            var permissionLauncher = new ScreenCapturePermissionLauncher();
            var permissionResult = await permissionLauncher.RequestPermissionAsync(cancellationToken).ConfigureAwait(false);
            if (!permissionResult.IsGranted || permissionResult.PermissionData is not Intent permissionData)
            {
                return ScreenRecordingStartResult.Failure(permissionResult.ErrorMessage ?? "Screen capture permission was not granted.");
            }

            var requestId = Guid.NewGuid().ToString("N");
            var startCompletion = new TaskCompletionSource<ScreenRecordingStartResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!PendingStarts.TryAdd(requestId, startCompletion))
            {
                return ScreenRecordingStartResult.Failure("Unable to initialize recording start request.");
            }

            ScreenRecordService.StartRecording(
                activity,
                requestId,
                permissionResult.ResultCode,
                permissionData,
                enableMicrophone);

            using var registration = cancellationToken.Register(() =>
            {
                if (PendingStarts.TryRemove(requestId, out var pending))
                {
                    pending.TrySetResult(ScreenRecordingStartResult.Failure("Recording start was canceled."));
                }
            });

            var startResult = await startCompletion.Task.ConfigureAwait(false);
            if (startResult.IsSuccess)
            {
                _isRecording = true;
                _isStopRequested = false;
                _recordingStartedAt = DateTimeOffset.UtcNow;
            }

            return startResult;
        }
        finally
        {
            StartStopLock.Release();
        }
    }

    public partial async Task<ScreenRecordingStopResult> StopAsync(CancellationToken cancellationToken = default)
    {
        await StartStopLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!_isRecording)
            {
                return ScreenRecordingStopResult.Failure("Recording is not currently running.");
            }

            if (_isStopRequested)
            {
                return ScreenRecordingStopResult.Failure("Recording stop is already in progress.");
            }

            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return ScreenRecordingStopResult.Failure("Android activity is unavailable.");
            }

            _isStopRequested = true;
            var requestId = Guid.NewGuid().ToString("N");
            var stopCompletion = new TaskCompletionSource<ScreenRecordingStopResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!PendingStops.TryAdd(requestId, stopCompletion))
            {
                _isStopRequested = false;
                return ScreenRecordingStopResult.Failure("Unable to initialize recording stop request.");
            }

            ScreenRecordService.StopRecording(activity, requestId);

            using var registration = cancellationToken.Register(() =>
            {
                if (PendingStops.TryRemove(requestId, out var pending))
                {
                    pending.TrySetResult(ScreenRecordingStopResult.Failure("Recording stop was canceled."));
                }
            });

            var stopResult = await stopCompletion.Task.ConfigureAwait(false);
            _isRecording = false;
            _isStopRequested = false;

            if (!stopResult.IsSuccess && _recordingStartedAt.HasValue)
            {
                var duration = DateTimeOffset.UtcNow - _recordingStartedAt.Value;
                stopResult = stopResult with { Duration = duration };
            }

            _recordingStartedAt = null;
            RaiseRecordingStopped(stopResult);
            return stopResult;
        }
        finally
        {
            StartStopLock.Release();
        }
    }

    internal static void NotifyStartSucceeded(string requestId, bool isMicrophoneEnabled)
    {
        if (PendingStarts.TryRemove(requestId, out var completion))
        {
            completion.TrySetResult(ScreenRecordingStartResult.Success(isMicrophoneEnabled));
        }
    }

    internal static void NotifyStartFailed(string requestId, string errorMessage)
    {
        if (PendingStarts.TryRemove(requestId, out var completion))
        {
            completion.TrySetResult(ScreenRecordingStartResult.Failure(errorMessage));
        }
    }

    internal static void NotifyStopCompleted(string requestId, string? savedFilePath, TimeSpan duration)
    {
        if (PendingStops.TryRemove(requestId, out var completion))
        {
            if (string.IsNullOrWhiteSpace(savedFilePath))
            {
                completion.TrySetResult(ScreenRecordingStopResult.Failure("Recording stopped but output file path is unavailable."));
                return;
            }

            completion.TrySetResult(ScreenRecordingStopResult.Success(savedFilePath, duration));
        }
    }

    internal static void NotifyStopFailed(string requestId, string errorMessage)
    {
        if (PendingStops.TryRemove(requestId, out var completion))
        {
            completion.TrySetResult(ScreenRecordingStopResult.Failure(errorMessage));
        }
    }
}
