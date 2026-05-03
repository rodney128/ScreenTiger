namespace ScreenTiger;

public enum AiAssistantLaunchOutcome
{
    OpenedChatGptAndSent,
    OpenedChatGptWithoutAttachment,
    ChatGptNotInstalled,
    Failed
}

public sealed record AiAssistantLaunchResult(AiAssistantLaunchOutcome Outcome, string Message)
{
    public static AiAssistantLaunchResult ChatGptOpenedAndSent() =>
        new(
            AiAssistantLaunchOutcome.OpenedChatGptAndSent,
            "ChatGPT opened. ScreenTiger sent the AI report and recording. Tap Send if they appear.");

    public static AiAssistantLaunchResult ChatGptOpenedWithoutAttachment() =>
        new(
            AiAssistantLaunchOutcome.OpenedChatGptWithoutAttachment,
            "ChatGPT opened. The AI report was copied to your clipboard. Paste it into ChatGPT. The recording could not be attached automatically.");

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
