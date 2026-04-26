@echo off
REM === Capture pktmon - VERSION DEBUG (no filter) ===
REM Lance ce fichier en clic-droit > Executer en tant qu'administrateur

echo [*] Reset des filtres pktmon...
pktmon filter remove

echo [*] Capture sans filtre (on filtrera apres). Full payload.
pktmon start --capture --pkt-size 0 --file-name "%~dp0hystoria_capture.etl" --comp nics --type all

echo.
echo [*] Statut :
pktmon status

echo.
echo [OK] Capture en cours.
echo.
echo === LANCE MAINTENANT TON LAUNCHER HYSTORIA ===
echo - Connecte-toi
echo - Selectionne ton perso
echo - Fais 1-2 actions (deplacement, parler PNJ)
echo - PAS PLUS DE 2 MINUTES
echo.
echo Quand t'as fini, lance stop-capture.bat (en admin)
echo.
pause
