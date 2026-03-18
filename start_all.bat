@echo off
REM Script per avviare nodi blockchain e Windows Form nello stesso terminale

REM Entrare nella cartella go_blockchain
cd go_blockchain || (echo Cartella go_blockchain non trovata & exit /b)

REM Avviare central-node
echo Avvio central-node...
docker-compose up -d --build central-node

REM Attendere che central-node sia UP
echo Controllo stato central-node...
:CheckNode
REM Usa curl per interrogare l'API di health
curl -s http://localhost:8080/api/health | findstr /C:"\"status\": \"UP\""
IF %ERRORLEVEL% NEQ 0 (
    echo Nodo non ancora pronto, attendo 5 secondi...
    timeout /t 5 >nul
    goto CheckNode
)
echo Nodo central-node attivo.

REM Avviare node1
echo Avvio node1...
docker-compose up -d --build node1
timeout /t 5 >nul

REM Avviare node2
echo Avvio node2...
docker-compose up -d --build node2
timeout /t 5 >nul

REM Avviare node3
echo Avvio node3...
docker-compose up -d --build node3

REM Entrare nella cartella windows_form
cd ../windows_form || (echo Cartella windows_form non trovata & exit /b)

REM Avviare l'app .NET
echo Avvio Windows Form...
dotnet run

echo Tutti i processi avviati.
pause