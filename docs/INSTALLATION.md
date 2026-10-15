# Installation and upgrade

## Package and prerequisites

The release archive is artifacts/TrainingPortal-IIS.zip. The archive includes the application, generated IIS web.config, deployment scripts and documentation. Use the rebuild commands below to regenerate artifacts/publish. The final handover folder contains fresh IIS/source archives, a separate media archive, deployment scripts, documentation and validation evidence. Source remains in this workspace. The source sharing archive, artifacts/TrainingPortal-Source.zip, excludes Git history, generated builds, local runtime data and release binaries. Uploaded videos must be transferred separately when required. Both archives use standard ZIP paths and extract with Windows Explorer, macOS Archive Utility or `unzip`.

Use Windows Server 2022, IIS, SQL Server and the .NET 10 ASP.NET Core Hosting Bundle. Install IIS before the bundle (repair the bundle if installed first). Create an application pool with No Managed Code, Integrated pipeline, and a dedicated service identity. Bind the site to HTTPS with a trusted certificate. See [Microsoft IIS deployment guidance](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0).

Set ASPNETCORE_ENVIRONMENT=Production. Give the identity read access to the published application, write access to App_Data/Logs, and read/write/delete access to the private video repository for uploads and failed-upload cleanup. Create the configured DataProtection:KeyPath directory before starting IIS and give only the app-pool identity and backup administrators access (the app needs read/write). Keys are protected with Windows DPAPI for that identity. Keep that identity and key directory stable across upgrades. If no explicit key path is configured, use Load User Profile=True and retain the profile/key store across upgrades for cookie data protection. See [Microsoft advanced IIS configuration](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/advanced?view=aspnetcore-10.0).

## Database

1. Back up the existing database and stop application writes before upgrading.
2. Review deployment/Database.sql, an idempotent EF migration script. Apply it with SSMS or sqlcmd to the intended database using a DBA identity. The application does not automatically migrate on startup.
3. The new WorkerType defaults to Unknown. Verify each existing account and set it to Employee only for an eligible permanent employee. Unknown, Contractor, ThirdParty, Intern and Temporary cannot sign in. IDs beginning T/S remain blocked regardless of category.
4. Upgrade resets video completion flags where saved progress is shorter than the video's duration. Review historical quiz results that were earned under the old 95% rule; they are retained, not erased. Archived audit history does not exist for activity predating this upgrade.
5. Resolve duplicate active QuizResults per employee before the ProtectAssessmentHistory migration creates its unique filtered index. Do not silently discard attempts: have the training owner decide their disposition.
6. For a new database, use deployment/BootstrapAdministrator.sql after replacing its placeholders to create the first verified administrator employee record. The corporate app role is assigned separately in Entra.
7. Give the runtime SQL identity the required application table permissions. Restrict AuditLogs to SELECT/INSERT, with no UPDATE/DELETE for the runtime identity. Schema migration credentials should be separate.

The local development database has received AddAnswerKeywords. The client database must still receive the current deployment script before rollout.

## Corporate SSO

Copy deployment/appsettings.Production.example.json to appsettings.Production.json in the deployed application and replace every placeholder. Register the corporate web application with redirect URI https://YOUR-HOST/signin-oidc and sign-out callback https://YOUR-HOST/signout-callback-oidc. Supply the app registration credential securely through deployment configuration (EntraId__ClientSecret if using a secret); do not put production secrets in source control.

Configure tenant/client ID, Administrator and Employee app-role values, and the actual role claim type emitted by the tenant. Assign roles in Entra. MatchMode Email requires a unique trusted corporate claim matching Employees.Email; MatchMode UserId requires the configured claim to match Employees.UserId. The example claim names must be confirmed against actual tenant tokens and claims mapping. Both administrators and employees must have an eligible employee database record. There is no automatic first-user promotion.

An account with missing/ambiguous identity, unknown category or excluded ID is denied before the SSO cookie is issued. Eligibility is rechecked on authenticated application requests so new exclusions apply to existing sessions. Keep PortalAuthorization:AllowDevelopmentBypass false in all deployed environments.

## Videos

Set VideoRepository:Path to a private directory, for example D:\TrainingPortalData\Videos. Do not expose it as an IIS virtual directory or put it under wwwroot. Place MP4/WebM content there and enter a relative filename such as safety/intro.mp4 in video CRUD. External URLs are no longer playable: copy approved source media into the private repository and update the records before rollout. Published source code does not include the client's videos.

Enter the correct whole-second duration and verify each file in the target browser. Inaccurate duration metadata can prevent completion or misstate it; content acceptance must verify duration. Video titles and descriptions remain editable. Content, duration and order cannot be changed after viewing begins; create a new program for revised material.

## Verification and operation

Run the client acceptance checklist in TEST-REPORT.md with real corporate identities and at least two browser tabs. Verify SQL persistence across app-pool recycle, CSV output in workforce languages, video codecs, large files and proxy timeouts. Reports calculate summaries in SQL and display 100 rows per page; CSV exports include all matching rows. Load-test the expected workforce size before production.

Operational logs rotate daily under App_Data/Logs (30 retained files). Audit rows remain in SQL until an organization-approved retention/archive process runs; the portal provides no audit deletion endpoint. Back up database, private media, configuration and data-protection keys. An upgrade rollback requires a matching database backup and previous application package; do not restore old binaries against a changed schema without compatibility review.

## Rebuild

From the solution root:

~~~powershell
dotnet restore ITC.TrainingPortal.sln
dotnet test ITC.TrainingPortal.Tests --logger 'trx;LogFileName=compliance-tests.trx' --results-directory artifacts/test-results
dotnet ef migrations script --idempotent --project ITC.TrainingPortal.Infrastructure --startup-project ITC.TrainingPortal.Web --output deployment/Database.sql
dotnet publish ITC.TrainingPortal.Web -c Release -o artifacts/publish
~~~


## Typed-answer upgrade

Before deploying the typed-answer feature, back up the database and apply the updated idempotent `deployment/Database.sql` (migration `AddTypedAnswerReview`). This adds question format and manual-review fields. Existing questions remain multiple choice and existing results retain their outcomes. The migration script is supplied; applying it to the target SQL Server is a deployment step.

The later `PersistShuffledQuestionOrder` migration adds the saved order for each exam attempt. Apply the current `deployment/Database.sql` before using question shuffling. Existing completed results remain unchanged; an active attempt from an older version receives and saves its shuffled order when it is first resumed.


## .NET 10 build and deployment

Install the .NET SDK 10.0.401 (or a newer 10.0.4xx patch allowed by global.json) on build machines. Run `dotnet tool restore`, `dotnet restore`, `dotnet test ITC.TrainingPortal.sln`, then `dotnet publish ITC.TrainingPortal.Web -c Release -o artifacts/publish-net10`.

The IIS server requires the .NET 10 ASP.NET Core Hosting Bundle, version 10.0.13 or a later servicing release. A desktop runtime alone is insufficient. Deploy the supplied `TrainingPortal-IIS.zip` or a newly published .NET 10 output directory. Keep SQL Server configuration, private video storage and data-protection keys when deploying.

Developer machines should install the SDK required by `global.json`.


The `AddAnswerKeywords` migration adds a non-null `AnswerKeywords` column to QuizQuestions, with an empty default for existing questions. Apply the updated `deployment/Database.sql` before starting this version. Existing typed questions keep manual review until keywords can be configured (questions with attempts remain locked). This implementation does not apply migrations to your database automatically.


## SQL script session settings

The supplied Database.sql begins with the session options required by SQL Server filtered indexes. Preserve this preamble if regenerating the EF script. When running a newly generated raw EF script with sqlcmd, use `-I` to enable QUOTED_IDENTIFIER (and `-b` to stop on errors). The final supplied script was tested against a fresh isolated database and then applied a second time.
