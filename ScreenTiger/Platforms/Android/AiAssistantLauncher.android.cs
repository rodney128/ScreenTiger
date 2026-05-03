using Android.Content;
using AndroidX.Core.Content;
using Java.IO;
using Microsoft.Maui.ApplicationModel;

namespace ScreenTiger;

public sealed partial class AiAssistantLauncher
{
    private const string ChatGptPackageName = "com.openai.chatgpt";

    public partial Task<AiAssistantLaunchResult> SendToChatGptAsync(string filePath, string reportText, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportText);

        var context = Platform.AppContext;
        var authority = $"{AppInfo.Current.PackageName}.fileprovider";
        var fileUri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, authority, new Java.IO.File(filePath));

        var targetedIntent = CreateBaseSendIntent(fileUri, reportText);
        targetedIntent.SetPackage(ChatGptPackageName);

        if (targetedIntent.ResolveActivity(context.PackageManager) is not null)
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

        var shareIntent = CreateBaseSendIntent(fileUri, reportText);
        var chooser = Intent.CreateChooser(shareIntent, "Send recording and report");
        chooser.AddFlags(ActivityFlags.NewTask);

        if (chooser.ResolveActivity(context.PackageManager) is not null)
        {
            try
            {
                context.StartActivity(chooser);
                return Task.FromResult(AiAssistantLaunchResult.ShareSheetOpened());
            }
            catch (ActivityNotFoundException)
            {
            }
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
