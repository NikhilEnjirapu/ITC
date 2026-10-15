param([string]$SqlServer = 'localhost\SQLEXPRESS')
$ErrorActionPreference = 'Stop'
if ($SqlServer -notmatch '^(localhost|\.|\(local\))(\\[A-Za-z0-9_]+)?$') {
    throw 'Local demo only: specify localhost, ., or (local), optionally followed by an instance name.'
}
$previousConnection = $env:ConnectionStrings__DefaultConnection
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    $env:ConnectionStrings__DefaultConnection = "Server=$SqlServer;Database=EmployeeTrainingPortalDemo;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
    Write-Host 'Open http://localhost:5121/login and use the Development Testing Only buttons. Press Ctrl+C to stop.'
    dotnet run --project ITC.TrainingPortal.Web --launch-profile http
    if ($LASTEXITCODE -ne 0) { throw 'The demo application exited with an error.' }
} finally {
    $env:ConnectionStrings__DefaultConnection = $previousConnection
    Pop-Location
}
