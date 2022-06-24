@echo off

cd "%~dp0"
cd ../

del MNet-Release.zip
7z a MNet-Release.zip MNet-Release

echo Gather Process Finished

PAUSE