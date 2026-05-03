namespace ScreenTiger;

public enum AiAssistantLaunchOutcome
{
    OpenedChatGpt,
    OpenedShareSheet,
    Failed
}

public sealed record AiAssistantLaunchResult(AiAssistantLaunchOutcome Outcome, string Message)
{
    public static AiAssistantLaunchResult ChatGptOpened() =>
        new(AiAssistantLaunchOutcome.OpenedChatGpt, "ChatGPT opened with your report and recording. Tap Send.");

    public static AiAssistantLaunchResult ShareSheetOpened() =>
        new(AiAssistantLaunchOutcome.OpenedShareSheet, "ChatGPT could not be opened directly. Android Share Sheet opened instead. Choose ChatGPT, then tap Send.");

    public static AiAssistantLaunchResult Failure() =>
        new(AiAssistantLaunchOutcome.Failed, "Could not open ChatGPT or Android Share Sheet. Copy the AI report and share the MP4 manually.");
}

public sealed partial class AiAssistantLauncher
{
    public partial Task<AiAssistantLaunchResult> SendToChatGptAsync(string filePath, string reportText, CancellationToken cancellationToken);
}

#if !ANDROID
public sealed partial class AiAssistantLauncher
{
    public partial Task<AiAssistantLaunchResult> SendToChatGptAsync(string filePath, string reportText, CancellationToken cancellationToken)
    {
        return Task.FromResult(AiAssistantLaunchResult.Failure());
    }
}
#endif
