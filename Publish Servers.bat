@echo off

cd %~dp0
cd MNet-Core\Master-Server
dotnet publish /p:PublishProfile="Linux" --nologo
dotnet publish /p:PublishProfile="Windows" --nologo

cd %~dp0
cd MNet-Core\Game-Server
dotnet publish /p:PublishProfile="Linux" --nologo
dotnet publish /p:PublishProfile="Windows" --nologo

Pause