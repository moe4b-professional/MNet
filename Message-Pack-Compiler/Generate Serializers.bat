@echo off

Set Source="..\Multiplayer-Game-VS\Fixed\Fixed.csproj"
Set Destination="..\Multiplayer-Game-VS\Fixed\Source\MessagePack-Generated.cs"

mpc.exe -i %Source% -o %Destination% -m

Pause