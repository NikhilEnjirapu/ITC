# Technical notes and requirement status

## Architecture

Browser/Razor/Bootstrap -> IIS/ASP.NET Core 8 -> application services -> EF Core -> SQL Server. Media is read through authenticated MVC endpoints from a private repository. Entra ID provides authentication; role policies separate Administrator and Employee. Local ASP.NET Identity user/password storage is not needed for the corporate SSO-only flow. Serilog records operational requests/errors; SQL AuditLogs records application activity.

## Compliance implementation

- Video count: unrestricted administrator CRUD, per the client's clarification. At least one video is needed for quiz unlock.
- EligibilityEvaluator always blocks T/S prefixes and all non-Employee categories, including unknown categories. Additional exclusion rows are managed by EligibilityController and evaluated at SSO token validation and each authenticated request.
- Audit writes accompany EF entity changes in the same SaveChanges transaction on SQL Server, including previous/current scalar values. Portal request access and login allow/deny are separate events. Submitted answers are persisted with the result. Existing rows do not retroactively acquire historical audit detail.
- Viewing checks assignment and prerequisite completion on open, heartbeat and media access. Server progress accrues at elapsed normal-speed time, bounded to a 20-second credit window; idle gaps over 20 seconds expire. A one-second timing tolerance retains debt so repeated requests cannot accumulate extra credit. Full configured duration is required for completion. SQL rowversion prevents concurrent progress overwrites.
- The browser prevents forward seeking/rate changes, sends progress every two seconds and on pause/end, stops on failed access/network validation and reacts to an exam started in another tab. Polling also detects active attempts across devices. Media responses are private/no-store.
- Quiz access requires completion of all videos or the configured unlock video, according to the program setting. One active attempt per employee is enforced by a filtered SQL unique index. Result rowversion protects concurrent submissions. Questions/passing criteria become immutable after attempts exist. Scores and pass/fail are calculated server-side and raw submitted answers are retained.
- ComplianceReportService computes current assignment status using videos plus assessment results. Progress views and Excel/PDF use that shared definition. Excel is an Open XML workbook using literal inline strings, preventing formula injection. PDF uses [PDFsharp](https://docs.pdfsharp.net/PDFsharp/Topics/Fonts/Font-Resolving.html) with Windows fonts.

## Limits requiring acceptance testing

These controls cannot prove human attention or stop a determined authenticated user from copying already-delivered media or modifying their browser. A tab can have media buffered before a quiz starts; polling stops the standard player within approximately two seconds, and new media requests are checked, but bytes already delivered cannot be revoked. Strict immediate revocation/DRM is not claimed. High-assurance protected streaming would require a separately designed media/DRM service and client agreement.

Duration is administrator-entered, not extracted from the file. Verify media durations/codecs as part of content acceptance. In-memory EF tests verify behavior but do not prove SQL Server constraints, transactions or performance. Live Entra claims/credentials, IIS bindings, SQL connectivity, client media and UAT remain deployment inputs.

## Deliverables

Source, migrations, generated database script, published IIS package, configuration example, installation instructions, user manual, automated test report and handover agenda are supplied. Actual deployment, real-tenant SSO validation, client content acceptance and knowledge-transfer session are not represented as completed.
