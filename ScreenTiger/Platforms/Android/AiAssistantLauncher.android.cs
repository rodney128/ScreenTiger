using Android.Content;
using Microsoft.Maui.ApplicationModel;

namespace ScreenTiger;

public sealed partial class AiAssistantLauncher
{
    private const string ChatGptPackageName = "com.openai.chatgpt";

    public partial async Task<AiAssistantLaunchResult> OpenChatGptAsync(string filePath, string reportText, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportText);

        var context = Platform.AppContext;
        var packageManager = context.PackageManager;

        var launchIntent = packageManager?.GetLaunchIntentForPackage(ChatGptPackageName);
        if (launchIntent is null)
        {
            return AiAssistantLaunchResult.ChatGptNotInstalled();
        }

        try
        {
            launchIntent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(launchIntent);
            await Task.CompletedTask.ConfigureAwait(false);
            return AiAssistantLaunchResult.ChatGptOpened();
        }
        catch (ActivityNotFoundException)
        {
            return AiAssistantLaunchResult.Failure();
        }
    }
}
