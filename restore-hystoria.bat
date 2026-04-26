@echo off
REM === Restaure le config.xml original d'Hystoria ===

set CONFIG="C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\retroclient\config.xml"
set BACKUP="C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\retroclient\config.xml.backup"

if not exist %BACKUP% (
    echo [ERREUR] Backup introuvable : %BACKUP%
    echo Le setup-hystoria.bat n'a peut-etre jamais ete lance.
    pause
    exit /b 1
)

echo [*] Restauration du config.xml original...
copy /Y %BACKUP% %CONFIG%
if errorlevel 1 (
    echo [ERREUR] Restauration impossible. Le launcher Hystoria est-il en cours ?
    pause
    exit /b 1
)

echo [OK] config.xml original restaure.
echo Le launcher Hystoria pointera de nouveau directement sur les serveurs Hystoria.
pause
