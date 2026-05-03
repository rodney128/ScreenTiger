using Android.Content;
using AndroidX.Core.Content;
using Java.IO;
using Microsoft.Maui.ApplicationModel;

namespace ScreenTiger;

public sealed partial class AiAssistantLauncher
{
    private const string ChatGptPackageName = "com.openai.chatgpt";

    public partial Task<AiAssistantLaunchResult> OpenChatGptAsync(string filePath, string reportText, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportText);

        var context = Platform.AppContext;
        var packageManager = context.PackageManager;
        var authority = $"{AppInfo.Current.PackageName}.fileprovider";
        var fileUri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, authority, new Java.IO.File(filePath));

        var targetedIntent = CreateBaseSendIntent(fileUri, reportText);
        targetedIntent.SetPackage(ChatGptPackageName);

        if (targetedIntent.ResolveActivity(packageManager) is not null)
        {
            try
            {
                context.StartActivity(targetedIntent);
                return Task.FromResult(AiAssistantLaunchResult.ChatGptOpened());
            }
            catch (ActivityNotFoundException)
            {
            }
        }

        var launchIntent = packageManager?.GetLaunchIntentForPackage(ChatGptPackageName);
        if (launchIntent is not null)
        {
            try
            {
                launchIntent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(launchIntent);
                return Task.FromResult(AiAssistantLaunchResult.ChatGptDirectOpened());
            }
            catch (ActivityNotFoundException)
            {
            }
        }

        if (launchIntent is null)
        {
            return Task.FromResult(AiAssistantLaunchResult.ChatGptNotInstalled());
        }

        return Task.FromResult(AiAssistantLaunchResult.Failure());
    }

    private static Intent CreateBaseSendIntent(Android.Net.Uri fileUri, string reportText)
    {
        var intent = new Intent(Intent.ActionSend);
        intent.SetType("video/mp4");
        intent.PutExtra(Intent.ExtraStream, fileUri);
        intent.PutExtra(Intent.ExtraText, reportText);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
        intent.ClipData = ClipData.NewRawUri("ScreenTigerRecording", fileUri);
        return intent;
    }
}
