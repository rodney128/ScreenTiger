namespace ScreenTiger;

public enum AiAssistantLaunchOutcome
{
    OpenedChatGpt,
    ChatGptNotInstalled,
    Failed
}

public sealed record AiAssistantLaunchResult(AiAssistantLaunchOutcome Outcome, string Message)
{
    public static AiAssistantLaunchResult ChatGptOpened() =>
        new(AiAssistantLaunchOutcome.OpenedChatGpt, "ChatGPT opened.");

    public static AiAssistantLaunchResult ChatGptNotInstalled() =>
        new(AiAssistantLaunchOutcome.ChatGptNotInstalled, "ChatGPT is not installed.");

    public static AiAssistantLaunchResult Failure() =>
        new(AiAssistantLaunchOutcome.Failed, "ScreenTiger could not open ChatGPT.");
}

public sealed partial class AiAssistantLauncher
{
    public partial Task<AiAssistantLaunchResult> OpenChatGptAsync(string zipContentUri, string reportText, CancellationToken cancellationToken);
    public partial Task<AiAssistantLaunchResult> OpenChatGptAppAsync(CancellationToken cancellationToken);
}

#if !ANDROID
public sealed partial class AiAssistantLauncher
{
    public partial Task<AiAssistantLaunchResult> OpenChatGptAsync(string zipContentUri, string reportText, CancellationToken cancellationToken)
    {
        return Task.FromResult(AiAssistantLaunchResult.Failure());
    }

    public partial Task<AiAssistantLaunchResult> OpenChatGptAppAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(AiAssistantLaunchResult.Failure());
    }
}
#endif
