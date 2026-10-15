# Demonstrate on another Windows laptop

This GitHub repository contains source and local-demo setup. The client submission ZIPs, private media, database records, logs and keys are not part of the demo repository. Corporate SSO is not needed for the localhost demonstration.

## Prerequisites

- Git and the .NET SDK 10.0.401 or a newer 10.0.4xx patch accepted by global.json. A runtime alone cannot build the source.
- SQL Server Express with local instance `localhost\SQLEXPRESS` (or another installed local SQL Server instance).
- Microsoft `sqlcmd` available on PATH, and your Windows account permitted to create a local database.
- Internet access for cloning and NuGet restore. IIS is not required for this demo.

## Clone and prepare

Run in PowerShell:

```powershell
git clone https://github.com/NikhilEnjirapu/ITC.git
cd ITC
powershell -NoProfile -ExecutionPolicy Bypass -File .\deployment\Setup-LocalDemo.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\deployment\Start-LocalDemo.ps1
```

The execution-policy override applies only to that command. The setup script creates or updates **EmployeeTrainingPortalDemo**, applies all migrations and adds two synthetic demo accounts. It does not drop a database, import client records, or alter the default EmployeeTrainingPortal database.

For a local default SQL instance, supply `-SqlServer localhost` to BOTH scripts. The scripts reject remote servers. SQL Server must already be installed and running; the scripts do not install prerequisites.

Open **http://localhost:5121/login** on the same laptop. Under **Development Testing Only**, select Administrator or Employee. Use separate browser profiles for the two roles. These buttons do not work on a remote hostname or in Production mode.

## Prepare the demonstration content

A new demo database has two accounts and no programs, videos, questions or assignments. As Administrator:

1. Create a training program.
2. Upload an approved short MP4/WebM, then verify its duration and preview playback.
3. Create its exam, add questions (or download and fill the CSV template), and activate the exam.
4. Assign the program to the local employee account (employee ID 900002).
5. Sign in as Employee, watch the video, take the exam, and show results.
6. Switch back to Administrator to show reports and audit history.

GitHub does not copy the current laptop's database or uploads. Carry an approved demo video separately, or transfer the existing media ZIP privately if appropriate. The existing file ee29c92760d54aacbeeef515ab3e9f09.mp4 measures about 170 seconds; do not reuse its old 60-second duration setting.

## Verification and troubleshooting

```powershell
dotnet test ITC.TrainingPortal.sln -c Release
```

- `sqlcmd` not found: install the SQL command-line tool and reopen PowerShell.
- Cannot connect: check the SQL Server service and instance name; use the same server for both scripts.
- Access denied after demo login: rerun setup; both synthetic employee records must exist and remain eligible.
- SDK mismatch: install the SDK version family required by global.json.
- Port 5121 in use: stop the other local portal process before launching.

Never expose Development mode to the internet. For real client deployment, use the separate submission package and production SSO instructions.
