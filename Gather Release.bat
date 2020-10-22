@echo off

rmdir /s /q "MNet-Release"
mkdir "MNet-Release"

xcopy "MNet-Unity\MNet-Unity.unitypackage" "MNet-Release\" /I /Q /Y /F

xcopy "MNet-Core\Game-Server\bin\Release\Linux" "MNet-Release\Game-Server\Linux" /S /I /Q /Y /F
xcopy "MNet-Core\Game-Server\bin\Release\Windows" "MNet-Release\Game-Server\Windows" /S /I /Q /Y /F

xcopy "MNet-Core\Master-Server\bin\Release\Linux" "MNet-Release\Master-Server\Linux" /S /I /Q /Y /F
xcopy "MNet-Core\Master-Server\bin\Release\Windows" "MNet-Release\Master-Server\Windows" /S /I /Q /Y /F

xcopy "Version.txt" "MNet-Release\" /I /Q /Y /F

del MNet-Release.zip
7z a MNet-Release.zip MNet-Release

echo Gather Process Finished

PAUSE