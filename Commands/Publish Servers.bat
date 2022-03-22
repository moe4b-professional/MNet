@echo off

cd %~dp0
cd ../
cd MNet-Core\Master-Server
dotnet publish /p:PublishProfile="Portable" --nologo

cd %~dp0
cd ../
cd MNet-Core\Game-Server
dotnet publish /p:PublishProfile="Portable" --nologo

Pause