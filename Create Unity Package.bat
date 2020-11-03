@echo off

cd "C:\Program Files\Unity\2019.4.7f1\Editor\"
Unity.exe -batchmode -nographics -quit -exportPackage "Assets/MNet" "MNet-Unity.unitypackage"

Pause