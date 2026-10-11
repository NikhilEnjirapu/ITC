# Validation report

## Release verification — 2026-10-11

- Solution build: 0 warnings, 0 errors (Debug and Release).
- .NET tests: 194 passed, 0 failed, 0 skipped. Evidence included at validation/release-20261011.trx in the IIS ZIP.
- JavaScript playback tests: 2 passed, 0 failed.
- Source archive: extracted to an empty folder, then built and tested from there with the same results (194 .NET, 2 JavaScript tests passed).
- IIS archive: extracted to an empty folder and started with `dotnet ITC.TrainingPortal.Web.dll` under both Development and Production environments, with no errors logged. /login and /privacy returned 200; favicon, logo, CSS, JavaScript and Bootstrap assets returned 200; anonymous / , /admin and /employee redirected to sign-in; the development login route /login/dev returned 404. This was a local Kestrel check, not target IIS or corporate SSO acceptance.
- No database changes since the September 21 release; Database.sql was not regenerated.

## Release verification — 2026-09-21

- Current source published successfully in Release mode to artifacts/client-release-20260921.
- .NET tests: 194 passed, 0 failed, 0 skipped. Evidence included at validation/release-20260921.trx in the ZIP.
- JavaScript playback tests: 2 passed, 0 failed.
- Deployment SQL regenerated with all eight migrations, including AddProgramExamUnlockCondition. Not executed against a fresh/client database.
- Prior local authenticated HTTP audit passed main admin/employee pages and Excel/PDF responses. This is not target IIS or corporate SSO acceptance.
- See RELEASE-NOTES.md for known application issues and outstanding acceptance checks.

## Historical validation — September 8

Date: 2026-09-08. Environment: local Windows, .NET SDK 8.0.424.

## Completed checks

- Solution build succeeded with no compiler warnings/errors.
- Automated suite: 95 passed, 0 failed, 0 skipped. Evidence: artifacts/test-results/compliance-tests.trx.
- Existing tests cover authentication/role foundations, route separation, employee/department and training management, assignment boundaries, viewing and quiz workflows.
- Added tests cover mandatory T/S and worker-category exclusions, all configurable rule types and deactivation, request denial/auditing, existing-session revocation, audit actor/value persistence, full-duration completion, pause saving, idle/paused manipulation rejection, consolidated completion/filtering, Open XML literal-string safety and multi-page PDF parsing.
- Release publish succeeded and generated an IIS AspNetCoreModuleV2 in-process web.config.
- Idempotent SQL migration script generated and inspected for worker category, progress reset, assessment history and active-attempt uniqueness changes. It was not executed against the project database.
- JavaScript syntax check passed.
- Published-app HTTP smoke: /login returned 200; anonymous requests to /admin, /employee/training, /employee/media/1 and /admin/reports/Excel redirected to login. This was a local Kestrel check, not an IIS or authenticated UAT run.
- The sandbox initially denied data-protection key writes. After adding a configurable DataProtection:KeyPath and running the local smoke process with reviewed permissions, Windows DPAPI key creation and startup succeeded. HTTP evidence is in artifacts/test-results/http-smoke.json. Installation instructions require creating and granting access to the selected key directory under the deployed service identity.

## Client acceptance still required

| Case | Expected result | Status |
| --- | --- | --- |
| Eligible Employee and Administrator corporate SSO | Correct role landing; LoginAllowed audit | Pending real tenant |
| T/S, contractor, third-party, intern, temporary and unknown accounts | Denied before portal access; denial audit | Pending real tenant |
| New admin exclusion against a signed-in user | Next request denied | Pending browser/UAT |
| Actual video files and declared durations | Files play in target browsers and full duration is required | Pending client media |
| Seek/speed changes, pause/resume, multiple tabs and network loss | Controls enforced; saved progress persists | Pending browser/UAT |
| Start exam with video already open | Standard player stops; new content requests blocked | Pending browser/UAT |
| Correct/incorrect answers and repeated submission | Server score/pass-fail, preserved answers, no duplicate completion | Pending SQL/browser UAT |
| Concurrent playback and quiz start | SQL rowversion/unique index enforce consistency | Pending SQL Server integration |
| Excel/PDF filtered exports including workforce languages | Correct data and readable font rendering | Pending client acceptance |
| App-pool restart and backup/restore | Persistent progress, keys and audit history | Pending IIS deployment |
| Expected employee count / concurrent users / long videos | Acceptable performance | Pending load test |

EF in-memory tests do not validate SQL Server transaction rollback or concurrency constraints. No claim is made that the client environment is deployed or fully accepted. Browser controls cannot revoke media bytes already delivered or prevent screen recording; see TECHNICAL.md.
