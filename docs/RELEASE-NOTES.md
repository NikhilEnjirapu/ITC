# Best of three attempts ? client requirement ? 2026-10-15

- Employees may complete an exam up to three times, including after passing; using all three attempts is optional.
- The highest graded score determines the reported exam outcome. A later lower score does not remove an earlier pass.
- Answers awaiting manual review count toward the three-attempt limit but do not enter the best-score calculation until graded.
- All attempt results remain available. The results page separates the best score from individual attempt history.
- A fourth attempt is blocked by the server. Existing video prerequisites and exam activation rules still apply.
- No database migration is needed for this policy clarification.

# Question shuffling - 2026-10-14

- Each new exam attempt receives an independently shuffled question sequence.
- The sequence is stored with the attempt, so refresh and resume do not reorder questions.
- Scoring and manual review continue to use question IDs; administrator review follows the employee's saved sequence.
- Apply the updated `deployment/Database.sql` before deployment (`PersistShuffledQuestionOrder`).

# .NET 10 upgrade - 2026-10-13

- All five projects now target .NET 10. SDK selection is pinned to 10.0.401 with servicing patch roll-forward.
- EF Core, ASP.NET Core MVC testing and the local EF tool use 10.0.12; Serilog.AspNetCore uses 10.0.0.
- Updated the isolated HTTP test host to remove the retained EF provider configuration before registering its in-memory database.
- EF reports no pending model changes. Deployment SQL was regenerated with EF 10; no database was changed.
- New Release output: artifacts/publish-net10. Prior release archives remain unchanged.
- IIS requires the .NET 10 Hosting Bundle. See INSTALLATION.md before deployment.

# Typed-answer feature - 2026-10-13

- Exams support multiple-choice and manually reviewed typed-answer questions in the same exam.
- Submitted descriptive answers show Awaiting review until an administrator publishes correct/incorrect marks; each question carries one point.
- Review identity, timestamp and marks are persisted. Pending results do not count as passed in compliance reports.
- Apply the updated deployment/Database.sql before deploying this source. The AddTypedAnswerReview migration preserves existing question formats and results.
- This source update does not rebuild the October 11 release archives or apply changes to a live database.

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
- Superseded by the client-confirmed best-of-three policy below: completed exams can be retaken while attempts remain and the exam is active.
- Corporate SSO, target IIS, fresh SQL installation, concurrency, browser media/download behavior, load and backup/restore acceptance remain pending.

This rebuild does not resolve the above application issues or constitute production acceptance. Client video files, database backups and live tenant credentials are not included.


## Project review fixes ? 2026-10-15

- Existing exam attempts remain visible and resumable after assignment removal, exam deactivation or unlock-rule changes. Starting a new attempt still requires an active assignment and current eligibility/unlock checks.
- Concurrent exam saves return recovery messages instead of unhandled errors. Question order is saved with the new attempt in one save.
- Previously earned completion and passing status remain intact during optional retakes.
- Dashboard readiness uses SQL existence checks. Compliance reports calculate summary values in SQL, filter before paging, and show 100 rows per page; CSV exports include all matching rows.
- Pin the transitive PKCS dependency and test XML dependency to 10.0.13. Deployed servers must run the .NET 10.0.13 Hosting Bundle or later.


## Follow-up assessment fixes ? 2026-10-15

- Assessment writes use serializable SQL transactions and transaction-owned application locks. Start/resume and submission coordinate per employee; start and question edits/imports/deletes coordinate per exam. Quiz settings changes use the same exam lock.
- New attempts recheck access, availability, unlock state and the three-attempt limit immediately before creation. Resuming an already-started attempt remains supported after assignment removal.
- Employee results history reads the employee's completed attempts independently of current assignments. This does not grant access to new attempts on unassigned programs.
- No schema migration is required. Deploy/restart all application instances together so every writer participates in the new transaction protocol.
