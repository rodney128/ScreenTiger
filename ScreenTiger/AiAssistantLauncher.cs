namespace ScreenTiger;

public enum AiAssistantLaunchOutcome
{
    OpenedChatGptDirect,
    ChatGptNotInstalled,
    Failed
}

public sealed record AiAssistantLaunchResult(AiAssistantLaunchOutcome Outcome, string Message)
{
    public static AiAssistantLaunchResult ChatGptDirectOpened() =>
        new(
            AiAssistantLaunchOutcome.OpenedChatGptDirect,
            "ChatGPT opened. The AI report was copied to your clipboard. Paste it into ChatGPT. Use View MP4 to confirm the recording if needed.");

    public static AiAssistantLaunchResult ChatGptNotInstalled() =>
        new(
            AiAssistantLaunchOutcome.ChatGptNotInstalled,
            "ChatGPT is not installed. Install ChatGPT, then try Open ChatGPT again.");

    public static AiAssistantLaunchResult Failure() =>
        new(AiAssistantLaunchOutcome.Failed, "ScreenTiger could not open ChatGPT. The AI report was copied to your clipboard.");
}

public sealed partial class AiAssistantLauncher
{
    public partial Task<AiAssistantLaunchResult> OpenChatGptAsync(string filePath, string reportText, CancellationToken cancellationToken);
}

#if !ANDROID
public sealed partial class AiAssistantLauncher
{
    public partial Task<AiAssistantLaunchResult> OpenChatGptAsync(string filePath, string reportText, CancellationToken cancellationToken)
    {
        return Task.FromResult(AiAssistantLaunchResult.Failure());
    }
}
#endif
