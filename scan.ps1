$path = 'C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\main.jsc'
$bytes = [System.IO.File]::ReadAllBytes($path)
$text = [System.Text.Encoding]::ASCII.GetString($bytes)
$pattern = '(?:[0-9]{1,3}\.){3}[0-9]{1,3}|[a-zA-Z0-9\-]+\.(?:com|fr|net|org|io|gg|co)'
$matches = [regex]::Matches($text, $pattern)
$matches | ForEach-Object { $_.Value } | Sort-Object -Unique
