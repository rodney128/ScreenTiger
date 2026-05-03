using Android.Content;
using AndroidX.Core.Content;
using Java.IO;
using Microsoft.Maui.ApplicationModel;

namespace ScreenTiger;

public sealed partial class RecordingFileViewer
{
    public partial Task<FileOpenResult> OpenSavedMp4Async(string? filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.FromResult(FileOpenResult.Failure("No saved recording is available to view."));
        }

        if (!System.IO.File.Exists(filePath))
        {
            return Task.FromResult(FileOpenResult.Failure("The saved MP4 could not be found. Record again and try View MP4."));
        }

        var context = Platform.AppContext;
        var authority = $"{AppInfo.Current.PackageName}.fileprovider";
        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, authority, new Java.IO.File(filePath));

        var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(uri, "video/mp4");
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);

        if (intent.ResolveActivity(context.PackageManager) is null)
        {
            return Task.FromResult(FileOpenResult.Failure("No video player could open this MP4 on this device."));
        }

        try
        {
            context.StartActivity(intent);
            return Task.FromResult(FileOpenResult.Success());
        }
        catch (ActivityNotFoundException)
        {
            return Task.FromResult(FileOpenResult.Failure("No video player could open this MP4 on this device."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(FileOpenResult.Failure($"Unable to open the saved MP4: {ex.Message}"));
        }
    }
}
