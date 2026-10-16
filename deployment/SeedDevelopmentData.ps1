$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\ITC.TrainingPortal.Web\ITC.TrainingPortal.Web.csproj'
$seedOutput = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\artifacts\development-seed\bin\'))
# A separate output directory permits setup while the local portal is running.
dotnet run --project $project --no-launch-profile "--property:BaseOutputPath=$seedOutput" -- --environment Development --seed-development-data
if ($LASTEXITCODE -ne 0) { throw 'Development data setup failed. See the output above.' }
