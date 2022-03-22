@echo off

cd %~dp0
cd ../

rmdir /s /q "MNet-Release"
mkdir "MNet-Release"

xcopy "MNet-Unity\MNet-Unity.unitypackage" "MNet-Release\" /I /Q /Y /F
xcopy "MNet-Core\Game-Server\bin\Release\Portable" "MNet-Release\Game-Server" /S /I /Q /Y /F
xcopy "MNet-Core\Master-Server\bin\Release\Portable" "MNet-Release\Master-Server" /S /I /Q /Y /F

xcopy "Version.txt" "MNet-Release\" /I /Q /Y /F

del MNet-Release.zip
7z a MNet-Release.zip MNet-Release

echo Gather Process Finished

PAUSE