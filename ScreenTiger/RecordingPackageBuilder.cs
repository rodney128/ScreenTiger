public sealed partial class RecordingPackageBuilder
{
    /// <summary>
    /// Creates a recording package ZIP that includes recording.mp4 and ai-report.txt in a user-facing public folder.
    /// </summary>
    public partial Task<RecordingPackageResult> CreatePackageAsync(
        string recordingFilePath,
        string reportText,
        string packageFileName,
        CancellationToken cancellationToken = default);
}

public sealed partial class RecordingPackageBuilder
{
    public partial Task<RecordingPackageResult> CreatePackageAsync(
        string recordingFilePath,
        string reportText,
        string packageFileName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RecordingPackageResult.Failure(packageFileName, "Download/ScreenTiger", null, "Recording package ZIP is only supported on Android."));
    }
}

public sealed record RecordingPackageResult(
    bool IsSuccess,
    string PackageFileName,
    string PackageDisplayFolder,
    string? PackageContentUri = null,
    string? ErrorMessage = null)
{
    public static RecordingPackageResult Success(string packageFileName, string packageDisplayFolder, string? packageContentUri) =>
        new(true, packageFileName, packageDisplayFolder, packageContentUri);

    public static RecordingPackageResult Failure(string packageFileName, string packageDisplayFolder, string? packageContentUri, string errorMessage) =>
        new(false, packageFileName, packageDisplayFolder, packageContentUri, errorMessage);
}
