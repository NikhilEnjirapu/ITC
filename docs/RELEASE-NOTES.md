# Client review package — 2026-10-11

This package is rebuilt from the current source and replaces the September 21 archive. No database changes: Database.sql and the eight migrations are unchanged, so no upgrade script needs to be applied for this release.

## Changes

- Reduced typography across the portal. Body text, tables, buttons and form fields are now 14px (previously 16px); labels, badges and table headers are 12–13px; headings follow a graded scale (page title 20px, sections 18px, cards 16px) instead of a uniform 24px. Table, button and field spacing was tightened to match. Form fields stay at 16px on screens narrower than 600px so iOS Safari does not zoom on focus. All sizes are defined once as CSS variables (`--fs-*` in site.css).
- Browser tab icon (favicon.ico) and home-screen icon (images/apple-touch-icon.png) now use the ITC logo mark.
- Removed unused template files from the source: the unused `_Layout`/`_Sidebar` views and their stylesheet, empty `Class1.cs` placeholders and a duplicate logo image. Build output (bin/obj) and uploaded videos are no longer tracked in source control.
- Both archives now use standard ZIP paths, so they extract correctly on Windows, macOS and Linux. Earlier archives stored Windows-style backslash paths.

The known issues and pending acceptance work listed under the September 21 package below are unchanged.

# Client review package — 2026-09-21

This package is rebuilt from the current source. It replaces the September 8 archive and includes the latest program exam-unlock migration, updated UI, and recent workflow/export/playback fixes. Requires the .NET 8 ASP.NET Core Hosting Bundle; this is a framework-dependent IIS deployment.

Start with INSTALLATION.md. Configure production SSO, SQL connectivity, private media storage, data-protection keys and permissions before use. Database.sql is idempotent and includes all eight migrations. It was regenerated, not applied to a client database. Back up an existing database before upgrades.

## Known issues and acceptance work

- Administrator self-deletion is not prevented when there is no learning history.
- Assignment removal or exam deactivation during active attempts can prevent normal resume while the active attempt still blocks learning.
- Adding videos to used programs can change completion status. Program deletion removes operational learning history; assignment removal hides retained results from current-assignment listings.
- Media replacement/deletion does not clean up old stored files.
- Video count is unrestricted and selected-video exam unlock is supported. These differ from the supplied PID and require agreement.
- Completed exams, including failures, cannot be retaken; agree the remediation process.
- Corporate SSO, target IIS, fresh SQL installation, concurrency, browser media/download behavior, load and backup/restore acceptance remain pending.

This rebuild does not resolve the above application issues or constitute production acceptance. Client video files, database backups and live tenant credentials are not included.
