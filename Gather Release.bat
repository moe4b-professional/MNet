@echo off

rmdir /s /q "MNet-Release"
mkdir "MNet-Release"

xcopy "MNet-Unity\MNet-Unity.unitypackage" "MNet-Release\" /I /Q /Y /F

xcopy "MNet-Core\GameServer\bin\Release\Linux" "MNet-Release\GameServer\Linux" /S /I /Q /Y /F
xcopy "MNet-Core\GameServer\bin\Release\Windows" "MNet-Release\GameServer\Windows" /S /I /Q /Y /F

xcopy "MNet-Core\MasterServer\bin\Release\Linux" "MNet-Release\MasterServer\Linux" /S /I /Q /Y /F
xcopy "MNet-Core\MasterServer\bin\Release\Windows" "MNet-Release\MasterServer\Windows" /S /I /Q /Y /F

xcopy "Version.txt" "MNet-Release\" /I /Q /Y /F

del MNet-Release.zip
7z a MNet-Release.zip MNet-Release

echo Gather Process Finished

PAUSE