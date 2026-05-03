using Android.Content;
using Android.OS;
using Android.Provider;
using Environment = Android.OS.Environment;
using System.IO.Compression;

namespace ScreenTiger;

public sealed partial class RecordingPackageBuilder
{
    public partial async Task<RecordingPackageResult> CreatePackageAsync(
        string recordingFilePath,
        string reportText,
        string packageFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordingFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportText);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageFileName);

        const string publicFolder = "Download/ScreenTiger";

        if (!System.IO.File.Exists(recordingFilePath))
        {
            return RecordingPackageResult.Failure(packageFileName, publicFolder, "The saved MP4 could not be found.");
        }

        var resolver = Android.App.Application.Context.ContentResolver;
        if (resolver is null)
        {
            return RecordingPackageResult.Failure(packageFileName, publicFolder, "Unable to access Android ContentResolver for ZIP export.");
        }

        var values = new ContentValues();
        values.Put(MediaStore.MediaColumns.DisplayName, packageFileName);
        values.Put(MediaStore.MediaColumns.MimeType, "application/zip");
        values.Put(MediaStore.MediaColumns.RelativePath, Path.Combine(Environment.DirectoryDownloads, "ScreenTiger"));
        values.Put(MediaStore.MediaColumns.IsPending, 1);

        var zipUri = resolver.Insert(MediaStore.Downloads.ExternalContentUri, values);
        if (zipUri is null)
        {
            return RecordingPackageResult.Failure(packageFileName, publicFolder, "Unable to create ZIP package entry in MediaStore.");
        }

        try
        {
            await using var outputStream = resolver.OpenOutputStream(zipUri, "w")
                ?? throw new InvalidOperationException("Unable to open ZIP package output stream.");

            using (var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var recordingEntry = zipArchive.CreateEntry("recording.mp4", CompressionLevel.Optimal);
                await using (var recordingEntryStream = recordingEntry.Open())
                await using (var recordingInputStream = System.IO.File.OpenRead(recordingFilePath))
                {
                    await recordingInputStream.CopyToAsync(recordingEntryStream, cancellationToken).ConfigureAwait(false);
                }

                var reportEntry = zipArchive.CreateEntry("ai-report.txt", CompressionLevel.Optimal);
                await using (var reportEntryStream = reportEntry.Open())
                await using (var reportWriter = new StreamWriter(reportEntryStream))
                {
                    await reportWriter.WriteAsync(reportText.AsMemory(), cancellationToken).ConfigureAwait(false);
                    await reportWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            var completeValues = new ContentValues();
            completeValues.Put(MediaStore.MediaColumns.IsPending, 0);
            resolver.Update(zipUri, completeValues, null, null);

            return RecordingPackageResult.Success(packageFileName, publicFolder);
        }
        catch (Exception ex)
        {
            try
            {
                resolver.Delete(zipUri, null, null);
            }
            catch
            {
            }

            return RecordingPackageResult.Failure(packageFileName, publicFolder, ex.Message);
        }
    }
}
