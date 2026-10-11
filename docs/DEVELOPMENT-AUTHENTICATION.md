# Local Development browser authentication

This mechanism tests the existing Admin and Employee UI before corporate SSO is configured. It issues a real cookie through the existing `Cookies` scheme and uses the existing role policies, employee resolver and eligibility middleware. It does not create database records or grant anonymous access.

## Local setup (first time)

1. Install SQL Server Express (instance `localhost\SQLEXPRESS`) or change `ConnectionStrings:DefaultConnection` in `ITC.TrainingPortal.Web/appsettings.json`.
2. Create an empty `EmployeeTrainingPortal` database and run `deployment/Database.sql` against it.
3. Run `deployment/DevelopmentLoginAccounts.sql` against the same database. Skipping this step makes both sign-in buttons end on **Access denied**.

The portal then opens with no programs, videos or exams; create them from the Administrator pages.

## Run and sign in

From the solution directory:

```powershell
dotnet run --project .\ITC.TrainingPortal.Web --launch-profile http
```

Open `http://localhost:5121/login`. The **Development Testing Only** section provides:

| Button | POST URL | Successful sign-in redirect |
| --- | --- | --- |
| Sign in as Administrator | `http://localhost:5121/login/dev/admin` | `/admin` |
| Sign in as Employee | `http://localhost:5121/login/dev/employee` | `/employee` |

Use the buttons: they submit anti-forgery-protected forms. Opening either URL with GET returns to the login page and does not authenticate. Sessions last at most 30 minutes and are not persistent across browser sessions. Logout posts to the existing `/logout` action and deletes the development cookie. Log out before switching roles, or use separate browser profiles for simultaneous Admin/Employee testing.

## Availability and authorization

Local sign-in is available only when `IWebHostEnvironment.IsDevelopment()` is true and `EntraId:Enabled` is false. MVC removes the controller entirely in other environments or when Entra is enabled, so both GET and POST routes return 404. Sign-in also checks these conditions at execution time and rejects non-loopback connections; it is intended for direct localhost use, not a remotely exposed Development site.

`PortalAuthorization:AllowDevelopmentBypass` remains **false**. The existing Admin and Employee policies still require authentication and the configured role claim. Each development identity receives only its chosen role. Admin does not gain Employee access and Employee does not gain Admin access.

The development cookie is named `.ITC.TrainingPortal.Development` and uses a development-specific ticket protection purpose. It cannot be decrypted by the normal production/Entra cookie handler even if a key store is shared. Outside the local Development branch, cookie configuration, corporate sign-in and corporate logout behavior are unchanged. No Entra ID configuration values were changed.

## Deterministic employee matching

The Development configuration uses the existing resolver:

```json
{
  "PortalAuthorization": {
    "AllowDevelopmentBypass": false,
    "RoleClaimType": "roles",
    "AdministratorRole": "Administrator",
    "EmployeeRole": "Employee"
  },
  "EmployeeIdentity": {
    "MatchMode": "Email",
    "EmailClaimType": "preferred_username"
  }
}
```

The controller reads those settings rather than replacing the policies or introducing a new identity model.

| Local choice | `roles` claim | `preferred_username` claim, matched to `Employees.Email` |
| --- | --- | --- |
| Administrator | `Administrator` | `local-admin@training-portal.test` |
| Employee | `Employee` | `local-employee@training-portal.test` |

If local configuration instead selects the existing `UserId` matching mode and specifies `UserIdClaimType`, that configured claim receives `training-portal-dev-admin` or `training-portal-dev-employee`, respectively, and matches `Employees.UserId`.

**A matching eligible employee database record is required for BOTH choices.** The current resolver queries `Employees`; the existing middleware also checks administrators. Without a matching record, sign-in can issue the cookie, but the redirected protected request is denied and the cookie is cleared by the existing middleware. The login buttons do not provision accounts or departments.

For local UI testing, run `deployment/DevelopmentLoginAccounts.sql` once against the local database, after `deployment/Database.sql`. It creates a "Development Testing" department and both records above (employee IDs 900001 and 900002, worker category `Employee`, matching email and user ID). It is safe to re-run and refuses to run before the schema exists. Sign-in itself never creates records, and the application has no eligibility exemption for these accounts. Never run this script against a client or production database.

Matching records permit the relevant UI to load. Employee training, exam and result lists can still be empty until actual local assignments/content/results exist. Missing data is not fabricated by authentication.

## Validation

Validation completed on 2026-09-09: **122 tests passed** (the existing 95 plus 27 new authentication cases), with no failures or skips. Debug (isolated output) and Release solution builds passed with zero warnings/errors. EF reported no pending model changes. No migration was created/applied and no SQL data was seeded. The test result is `artifacts/dev-auth-validation/results/development-authentication.trx`.

`DevelopmentAuthenticationTests` exercises real MVC requests, anti-forgery forms, cookie protection and authentication, the existing role policies and the existing employee resolver. Test employees exist only in a uniquely named EF in-memory database inside each test host; the fixture verifies the provider is InMemory before adding records. No test employee is inserted into SQL Server.

Tests cover both roles' requested pages, opposite-role denial, anonymous denial, logout, non-Development 404s, disabled routes/controls when Entra is enabled, loopback-only access, missing-record denial, custom role/UserId mappings, invalid mappings, and inability to reuse a development cookie in production.

The existing application process may lock its Debug executable on Windows. Validation can use a separate output directory without stopping that process:

```powershell
dotnet build ITC.TrainingPortal.sln --no-restore --property:BaseOutputPath=C:/Nikhil/ITC/artifacts/dev-auth-validation/bin/
dotnet test ITC.TrainingPortal.sln --no-build --no-restore --property:BaseOutputPath=C:/Nikhil/ITC/artifacts/dev-auth-validation/bin/
dotnet build ITC.TrainingPortal.sln -c Release --no-restore
```

The existing IIS release archive is not republished by this local-authentication task.
