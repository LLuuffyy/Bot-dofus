@echo off
REM === Arret pktmon + conversion ===

echo [*] Statut avant arret :
pktmon status

echo.
echo [*] Compteurs :
pktmon counters

echo.
echo [*] Arret de la capture...
pktmon stop

echo.
echo [*] Verification taille du .etl :
dir "%~dp0hystoria_capture.etl"

echo.
echo [*] Conversion ETL vers PCAPNG...
pktmon etl2pcap "%~dp0hystoria_capture.etl" --out "%~dp0hystoria_capture.pcapng"

echo.
echo [*] Resultat :
dir "%~dp0hystoria_capture.etl" "%~dp0hystoria_capture.pcapng"

echo.
echo [*] Nettoyage filtres...
pktmon filter remove

echo.
echo [OK] Termine.
pause
