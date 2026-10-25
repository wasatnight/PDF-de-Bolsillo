$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$app=Join-Path $projectRoot 'app/PdfDeBolsillo.exe'
$runRoot=Join-Path $projectRoot ('.verification-runs/'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'ejemplos') -Destination (Join-Path $runRoot 'ejemplos') -Recurse
$p=Start-Process -FilePath $app -ArgumentList '--verify' -WindowStyle Hidden -Wait -PassThru
if($p.ExitCode -ne 0){throw 'Falló la verificación de composición/idioma.'}
$p=Start-Process -FilePath $app -ArgumentList @('--verify-pdf',('"' + $runRoot + '"')) -WindowStyle Hidden -Wait -PassThru
if($p.ExitCode -ne 0){throw "Falló la verificación funcional. Revisa $runRoot"}
Get-Content -LiteralPath (Join-Path $runRoot 'verificacion-funcional/informe.json')
Write-Output "Resultados conservados para inspección: $runRoot"
