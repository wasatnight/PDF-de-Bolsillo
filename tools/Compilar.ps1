$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$project=Join-Path $projectRoot 'src/PdfDeBolsillo/PdfDeBolsillo.csproj'
dotnet restore $project --configfile (Join-Path $projectRoot 'NuGet.Config') -r win-x64 --locked-mode
if($LASTEXITCODE -ne 0){throw 'Falló la restauración'}
dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore -o (Join-Path $projectRoot 'app')
if($LASTEXITCODE -ne 0){throw 'Falló la compilación'}
New-Item -ItemType Directory -Force (Join-Path $projectRoot 'app/licencias') | Out-Null
Copy-Item -Path (Join-Path $projectRoot 'third_party/licenses/*') -Destination (Join-Path $projectRoot 'app/licencias') -Force
Write-Output (Join-Path $projectRoot 'app/PdfDeBolsillo.exe')
