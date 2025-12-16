@echo off
setlocal

set APP=%~dp0dist\StartDeck.exe
if not exist "%APP%" (
  echo dist\StartDeck.exe not found. Please run publish-win.bat first.
  exit /b 1
)

start "" "%APP%"
endlocal
