@echo off
REM === Setup Hystoria pour bot ===
REM Backup config.xml original puis le remplace par notre version patchee
REM Lancement : double-clic (pas besoin d'admin)

setlocal enabledelayedexpansion

set CONFIG="C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\retroclient\config.xml"
set BACKUP="C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\retroclient\config.xml.backup"
set PATCHED="%~dp0config.xml.patched"

echo === Setup Hystoria pour bot ===
echo.

if not exist %CONFIG% (
    echo [ERREUR] Fichier introuvable : %CONFIG%
    echo Le launcher Hystoria est-il bien installe a cet emplacement ?
    pause
    exit /b 1
)

if exist %BACKUP% (
    echo [INFO] Backup deja present : %BACKUP%
    echo [INFO] Je ne le remplace pas pour proteger ton original.
) else (
    echo [*] Creation du backup...
    copy %CONFIG% %BACKUP%
    if errorlevel 1 (
        echo [ERREUR] Backup impossible.
        pause
        exit /b 1
    )
    echo [OK] Backup cree : %BACKUP%
)

echo.
echo [*] Generation du config.xml patche...

(
echo ^<config^>
echo.
echo     ^<delay value="500"/^>
echo     ^<rdelay value="3000"/^>
echo     ^<rcount value="10"/^>
echo.
echo     ^<conf name="En ligne (Bot Local)" zaapconnectport="25000"^>
echo         ^<databank id="0"^>
echo             ^<dataserver url="data/" type="local" priority="4"/^>
echo             ^<dataserver url="http://51.178.36.51/dofus/" priority="3"/^>
echo             ^<dataserver url="http://51.178.36.51/dofus/" priority="0"/^>
echo         ^</databank^>
echo.
echo         ^<connserver name="Hystoria-Bot" ip="127.0.0.1" port="5555"/^>
echo.
echo         ^<servers^>
echo             ^<server id="601" ip="127.0.0.1" port="5556"/^>
echo         ^</servers^>
echo     ^</conf^>
echo.
echo     ^<cacheasbitmap^>
echo         ^<cache element="ExternalContainer/InteractionCell" value="true"/^>
echo         ^<cache element="ExternalContainer/Ground" value="true"/^>
echo         ^<cache element="ExternalContainer/Object1" value="true"/^>
echo         ^<cache element="ExternalContainer/Object2" value="true"/^>
echo         ^<cache element="ExternalContainer/Zone" value="true"/^>
echo         ^<cache element="ExternalContainer/Select" value="false"/^>
echo         ^<cache element="ExternalContainer/Grid" value="true"/^>
echo         ^<cache element="ExternalContainer/Pointer" value="true"/^>
echo         ^<cache element="GAPI/UI" value="false"/^>
echo         ^<cache element="GAPI/UITop" value="false"/^>
echo         ^<cache element="GAPI/Popup" value="false"/^>
echo         ^<cache element="GAPI/UIUltimate" value="false"/^>
echo         ^<cache element="GAPI/Cursor" value="true"/^>
echo         ^<cache element="mapHandler/BACKGROUND" value="false"/^>
echo         ^<cache element="mapHandler/Cell/Ground" value="false"/^>
echo         ^<cache element="mapHandler/Cell/Object1" value="false"/^>
echo         ^<cache element="mapHandler/Cell/Object2" value="false"/^>
echo         ^<cache element="mapHandler/Cell/ObjectExternal" value="false"/^>
echo         ^<cache element="Zone/Zone" value="true"/^>
echo         ^<cache element="Zone/Pointers" value="true"/^>
echo         ^<cache element="Controls/Loader" value="false" /^>
echo     ^</cacheasbitmap^>
echo.
echo ^</config^>
) > %PATCHED%

echo [OK] Patche : %PATCHED%

echo.
echo [*] Remplacement du config.xml live...
copy /Y %PATCHED% %CONFIG%
if errorlevel 1 (
    echo [ERREUR] Copie impossible. Le launcher Hystoria est-il en cours ? Ferme-le et relance.
    pause
    exit /b 1
)

echo [OK] config.xml remplace.
echo.
echo === Procedure de lancement ===
echo.
echo 1. Lance d'abord BotDofus.exe :
echo    "%~dp0temp-clone\bin\Debug\net8.0-windows\BotDofus.exe"
echo.
echo 2. Cree/selectionne un compte, coche "ModePassif" pour la 1ere fois
echo.
echo 3. Demarre le proxy (bouton dans l'UI)
echo.
echo 4. Lance ton launcher Hystoria normalement
echo    Le client va se connecter sur 127.0.0.1:5555 (notre proxy)
echo.
echo 5. Joue normalement. Tous les packets seront logges dans :
echo    "%~dp0temp-clone\bin\Debug\net8.0-windows\logs\"
echo.
echo === Pour revenir a l'utilisation normale ===
echo Lance le script restore-hystoria.bat.
echo.
pause
