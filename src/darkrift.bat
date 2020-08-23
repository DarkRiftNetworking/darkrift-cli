@echo off
REM
REM Start up script for Windows CMD/Powershell users
REM

SET dir=%~dp0
dotnet "%dir%darkrift-cli.dll" %*
