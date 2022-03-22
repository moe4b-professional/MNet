@echo off

cd ../

cd "C:\Program Files\Unity\Editor\2021.2.3f1\Editor"
Unity.exe -batchmode -nographics -quit -exportPackage "Assets/MNet" "MNet-Unity.unitypackage"

Pause