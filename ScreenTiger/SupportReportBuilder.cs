using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace ScreenTiger;

public static class SupportReportBuilder
{
    public static string BuildCompactReport(
        string filePath,
        TimeSpan? duration,
        bool? usedMicrophone,
        string? savedToLocation = null,
        string? publicContentUri = null,
        string? exportWarning = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string fileName = Path.GetFileName(filePath);
        string fileSizeText = TryGetFileSizeText(filePath);
        string durationText = duration.HasValue && duration.Value > TimeSpan.Zero
            ? duration.Value.ToString(@"hh\:mm\:ss")
            : "Unknown";
        string audioMode = usedMicrophone switch
        {
            true => "Microphone",
            false => "Video Only",
            null => "Unknown"
        };

        return string.Join(
            Environment.NewLine,
            "App: ScreenTiger",
            $"Package: {AppInfo.Current.PackageName}",
            $"Device: {DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}",
            $"Android: {DeviceInfo.Current.VersionString} (SDK {DeviceInfo.Current.Version.Major})",
            "Recording state/result: Saved",
            $"Saved file name: {fileName}",
            $"Saved file path: {filePath}",
            $"Saved to: {(string.IsNullOrWhiteSpace(savedToLocation) ? "Unknown" : savedToLocation)}",
            $"Public content URI: {(string.IsNullOrWhiteSpace(publicContentUri) ? "Unavailable" : publicContentUri)}",
            $"Export warning: {(string.IsNullOrWhiteSpace(exportWarning) ? "None" : exportWarning)}",
            $"File size: {fileSizeText}",
            $"Duration: {durationText}",
            $"Audio mode: {audioMode}",
            "Review request: Please review the attached ScreenTiger recording and identify UI/UX issues, workflow issues, errors, and likely root cause.");
    }

    private static string TryGetFileSizeText(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                return "Unknown";
            }

            double sizeInMb = fileInfo.Length / (1024d * 1024d);
            return $"{sizeInMb:F2} MB";
        }
        catch
        {
            return "Unknown";
        }
    }
}
