namespace ScreenTiger;

public enum AiAssistantLaunchOutcome
{
    OpenedChatGpt,
    OpenedChatGptDirect,
    Failed
}

public sealed record AiAssistantLaunchResult(AiAssistantLaunchOutcome Outcome, string Message)
{
    public static AiAssistantLaunchResult ChatGptOpened() =>
        new(AiAssistantLaunchOutcome.OpenedChatGpt, "ChatGPT opened with your report and recording. Tap Send.");

    public static AiAssistantLaunchResult ChatGptDirectOpened() =>
        new(
            AiAssistantLaunchOutcome.OpenedChatGptDirect,
            "ChatGPT opened. The AI report was copied to your clipboard. Paste it into ChatGPT. If the MP4 was not attached, use View MP4 or Share MP4.");

    public static AiAssistantLaunchResult ChatGptNotInstalled() =>
        new(
            AiAssistantLaunchOutcome.Failed,
            "ChatGPT is not installed. Install ChatGPT, or use View MP4 and copy the AI report manually.");

    public static AiAssistantLaunchResult Failure() =>
        new(AiAssistantLaunchOutcome.Failed, "ScreenTiger could not open ChatGPT. The AI report was copied to your clipboard. Use View MP4 to confirm the recording, then share the MP4 manually.");
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
