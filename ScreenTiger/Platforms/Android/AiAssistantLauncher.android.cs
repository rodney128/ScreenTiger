using Android.Content;
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

        var launchIntent = packageManager?.GetLaunchIntentForPackage(ChatGptPackageName);
        if (launchIntent is null)
        {
            return Task.FromResult(AiAssistantLaunchResult.ChatGptNotInstalled());
        }

        try
        {
            launchIntent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(launchIntent);
            return Task.FromResult(AiAssistantLaunchResult.ChatGptDirectOpened());
        }
        catch (ActivityNotFoundException)
        {
            return Task.FromResult(AiAssistantLaunchResult.Failure());
        }
    }
}
