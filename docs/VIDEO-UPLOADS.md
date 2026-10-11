# Administrator video uploads

Open **Admin > Training Videos > Add video**, choose an MP4 or WebM, select a training program, and enter the title and sequence number. The browser previews the selected file and fills the duration when metadata is available. Check that duration before saving. Create a training program first if the list is empty.

Edit supports an optional replacement upload. Leaving the file picker empty retains the existing filename. The existing restriction on replacing content after viewing has started still applies. Existing files are retained when replacing or deleting video records because other records may reference them.

Uploads are limited to 500 MiB (shown as 500 MB in the form). Server checks cover size, allowed extension, and a matching container header. These checks are not a full media decoder or malware scan; administrators should upload trusted, browser-compatible media. Duration comes from browser metadata or administrator input, not server-side media probing.

Files receive generated names and are stored in `VideoRepository:Path`, defaulting to `App_Data/Videos` under the application content root. The folder is created on the first accepted upload. Playback continues through the existing protected media endpoints. No database migration is required.

For IIS, use a persistent private directory outside the deployed application and grant the application pool identity read/write access to that directory. The upload buffer also requires writable temporary storage with sufficient capacity. Back up the video directory alongside the database. The source `web.config` allows 501 MiB requests for the training-video route, while the actions enforce the 500 MiB file limit. Existing deployed IIS overrides or reverse proxies may impose smaller limits and must be checked during deployment. See [Microsoft's upload documentation](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-8.0).

Failed application saves remove their newly uploaded file; existing files are never removed by that cleanup. A process interruption between file storage and database commit can leave an unreferenced file; filesystem and SQL writes are not a single transaction.
