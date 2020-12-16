@echo off
dotnet build
copy /Y "bin\Debug\netstandard2.0\ConsoleMod.dll" "..\Poly Bridge 2\BepInEx\plugins\ConsoleMod.dll"