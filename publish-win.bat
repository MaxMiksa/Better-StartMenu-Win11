@echo off
setlocal

REM Publish Native AOT single-file self-contained build to dist\
dotnet publish StartDeck.sln ^
  -r win10-x64 ^
  -c Release ^
  /p:PublishAot=true ^
  /p:PublishSingleFile=true ^
  /p:SelfContained=true ^
  -o dist

if %errorlevel% neq 0 (
  echo Publish failed.
  exit /b %errorlevel%
)

echo Publish succeeded. Output in dist\
endlocal
