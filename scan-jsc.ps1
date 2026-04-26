$bytes = [System.IO.File]::ReadAllBytes('C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\main.jsc')
$text = [System.Text.Encoding]::UTF8.GetString($bytes)
# Recherche large
$pattern = '\b162\.19\.\d{1,3}\.\d{1,3}\b|\b127\.0\.0\.1\b|\bhystoria\b|\bconnection\b|connserver|connexionServer|serverHost|FlashVars|loaderInfo|swfUrl|connectionPort'
$results = [regex]::Matches($text, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
Write-Host "Total matches: $($results.Count)"
$results | ForEach-Object { $_.Value } | Sort-Object -Unique | ForEach-Object { Write-Host $_ }
