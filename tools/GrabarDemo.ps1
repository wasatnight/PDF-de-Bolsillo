$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$build=Join-Path $root 'artifacts/demo-app'
$recording=Join-Path $root ('artifacts/demo-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $recording | Out-Null
dotnet publish (Join-Path $root 'src/PdfDeBolsillo/PdfDeBolsillo.csproj') -c Release -r win-x64 --self-contained true -p:RecordDemo=true -o $build
if($LASTEXITCODE -ne 0){throw 'Falló la compilación de la demo'}
$p=Start-Process -FilePath (Join-Path $build 'PdfDeBolsillo.exe') -ArgumentList @('--record-demo',('"' + $recording + '"'),'--demo-fixtures',('"' + (Join-Path $root 'ejemplos') + '"')) -WindowStyle Normal -Wait -PassThru
if($p.ExitCode -ne 0){throw "Falló la grabación. Revisa $recording"}
Get-Content (Join-Path $recording 'resultado.json')
Write-Output "Capturas PNG y PDF de prueba: $recording"
