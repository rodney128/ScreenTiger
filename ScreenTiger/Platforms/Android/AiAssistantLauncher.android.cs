using Android.Content;
using Microsoft.Maui.ApplicationModel;

namespace ScreenTiger;

public sealed partial class AiAssistantLauncher
{
    private const string ChatGptPackageName = "com.openai.chatgpt";

    public partial Task<AiAssistantLaunchResult> OpenChatGptAsync(string zipContentUri, string reportText, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipContentUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportText);

        var context = Platform.AppContext;
        var packageManager = context.PackageManager;
        var zipUri = Android.Net.Uri.Parse(zipContentUri);

        if (zipUri is null)
        {
            return Task.FromResult(AiAssistantLaunchResult.Failure());
        }

        var intent = new Intent(Intent.ActionSend);
        intent.SetPackage(ChatGptPackageName);
        intent.SetType("application/zip");
        intent.PutExtra(Intent.ExtraStream, zipUri);
        intent.PutExtra(Intent.ExtraText, reportText);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
        intent.ClipData = ClipData.NewUri(context.ContentResolver, "ScreenTiger recording package", zipUri);

        if (intent.ResolveActivity(packageManager) is null)
        {
            return Task.FromResult(AiAssistantLaunchResult.ChatGptNotInstalled());
        }

        try
        {
            context.StartActivity(intent);
            return Task.FromResult(AiAssistantLaunchResult.ChatGptOpened());
        }
        catch (ActivityNotFoundException)
        {
            return Task.FromResult(AiAssistantLaunchResult.ChatGptNotInstalled());
        }
        catch
        {
            return Task.FromResult(AiAssistantLaunchResult.Failure());
        }
    }

    public partial Task<AiAssistantLaunchResult> OpenChatGptAppAsync(CancellationToken cancellationToken)
    {
        var context = Platform.AppContext;
        var packageManager = context.PackageManager;

        var launchIntent = packageManager?.GetLaunchIntentForPackage(ChatGptPackageName);
        if (launchIntent is null)
        {
            return Task.FromResult(AiAssistantLaunchResult.ChatGptNotInstalled());
        }

        try
        {
            launchIntent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(launchIntent);
            return Task.FromResult(AiAssistantLaunchResult.ChatGptOpened());
        }
        catch (ActivityNotFoundException)
        {
            return Task.FromResult(AiAssistantLaunchResult.ChatGptNotInstalled());
        }
        catch
        {
            return Task.FromResult(AiAssistantLaunchResult.Failure());
        }
    }
}
