using Android.Content;
using AndroidX.Core.Content;
using Microsoft.Maui.ApplicationModel;

namespace ScreenTiger;

public sealed partial class AiAssistantLauncher
{
    private const string ChatGptPackageName = "com.openai.chatgpt";
    private const int ChatGptShareDelayMilliseconds = 1000;

    public partial async Task<AiAssistantLaunchResult> OpenChatGptAsync(string filePath, string reportText, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportText);

        var context = Platform.AppContext;
        var packageManager = context.PackageManager;
        var authority = $"{AppInfo.Current.PackageName}.fileprovider";
        var fileUri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, authority, new Java.IO.File(filePath));

        var launchIntent = packageManager?.GetLaunchIntentForPackage(ChatGptPackageName);
        if (launchIntent is null)
        {
            return AiAssistantLaunchResult.ChatGptNotInstalled();
        }

        try
        {
            launchIntent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(launchIntent);
        }
        catch (ActivityNotFoundException)
        {
            return AiAssistantLaunchResult.Failure();
        }

        await Task.Delay(ChatGptShareDelayMilliseconds, cancellationToken).ConfigureAwait(false);

        try
        {
            var shareIntent = new Intent(Intent.ActionSend);
            shareIntent.SetType("video/mp4");
            shareIntent.SetPackage(ChatGptPackageName);
            shareIntent.PutExtra(Intent.ExtraText, reportText);
            shareIntent.PutExtra(Intent.ExtraStream, fileUri);
            shareIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);
            shareIntent.ClipData = ClipData.NewRawUri("ScreenTigerRecording", fileUri);

            if (shareIntent.ResolveActivity(packageManager) is null)
            {
                return AiAssistantLaunchResult.ChatGptOpenedWithoutAttachment();
            }

            context.StartActivity(shareIntent);
            return AiAssistantLaunchResult.ChatGptOpenedAndSent();
        }
        catch (ActivityNotFoundException)
        {
            return AiAssistantLaunchResult.ChatGptOpenedWithoutAttachment();
        }
        catch (OperationCanceledException)
        {
            return AiAssistantLaunchResult.ChatGptOpenedWithoutAttachment();
        }
        catch
        {
            return AiAssistantLaunchResult.ChatGptOpenedWithoutAttachment();
        }
    }
}
