namespace ITC.TrainingPortal.Web.Services;

public sealed class VideoUploadService(IConfiguration configuration, IWebHostEnvironment environment)
{
    public const long MaxFileBytes = 500L * 1024 * 1024;
    public const long MaxRequestBytes = MaxFileBytes + 1024 * 1024;
    private string Root => Path.GetFullPath(configuration["VideoRepository:Path"] ??
        Path.Combine(environment.ContentRootPath, "App_Data", "Videos"));

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaxFileBytes)
            throw new InvalidOperationException("Choose a non-empty video no larger than 500 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".mp4" or ".webm"))
            throw new InvalidOperationException("Choose an MP4 or WebM video.");
        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var read = await input.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var recognized = read == 12 && (extension == ".mp4"
            ? header.AsSpan(4, 4).SequenceEqual("ftyp"u8)
            : header.AsSpan(0, 4).SequenceEqual(new byte[] { 0x1a, 0x45, 0xdf, 0xa3 }));
        if (!recognized) throw new InvalidOperationException("The file content does not match the selected video format. Choose an MP4 or WebM video.");

        Directory.CreateDirectory(Root);
        var name = Guid.NewGuid().ToString("N") + extension;
        var path = Path.Combine(Root, name);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await output.WriteAsync(header, cancellationToken);
            await input.CopyToAsync(output, cancellationToken);
            if (output.Length != file.Length || output.Length > MaxFileBytes)
                throw new InvalidOperationException("The upload was incomplete. Please choose the video again.");
            return name;
        }
        catch
        {
            File.Delete(path);
            throw;
        }
    }

    // Only names returned by SaveAsync may be passed here, never submitted filenames.
    public void RemoveNewUpload(string name) => File.Delete(Path.Combine(Root, name));
}
