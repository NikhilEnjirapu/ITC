param([string]$SqlServer = 'localhost\SQLEXPRESS')
$ErrorActionPreference = 'Stop'
if ($SqlServer -notmatch '^(localhost|\.|\(local\))(\\[A-Za-z0-9_]+)?$') {
    throw 'This script accepts only a local SQL Server instance. Never use it for client or production databases.'
}
Get-Command dotnet, sqlcmd -ErrorAction Stop | Out-Null
$demoDatabase = 'EmployeeTrainingPortalDemo'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Tool restore failed.' }
    dotnet restore ITC.TrainingPortal.sln
    if ($LASTEXITCODE -ne 0) { throw 'Package restore failed.' }
    sqlcmd -S $SqlServer -d master -E -C -I -b -Q "IF DB_ID(N'EmployeeTrainingPortalDemo') IS NULL CREATE DATABASE [EmployeeTrainingPortalDemo];"
    if ($LASTEXITCODE -ne 0) { throw 'Could not create the local demo database.' }
    sqlcmd -S $SqlServer -d $demoDatabase -E -C -I -b -i deployment/Database.sql
    if ($LASTEXITCODE -ne 0) { throw 'Database migration failed.' }
    sqlcmd -S $SqlServer -d $demoDatabase -E -C -I -b -i deployment/DevelopmentLoginAccounts.sql
    if ($LASTEXITCODE -ne 0) { throw 'Development account setup failed.' }
    Write-Host 'Demo setup completed. Run deployment/Start-LocalDemo.ps1 with the same -SqlServer value.'
} finally { Pop-Location }
