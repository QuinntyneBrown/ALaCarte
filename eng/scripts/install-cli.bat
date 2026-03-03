@echo off

set CLI_PROJECT=%~dp0..\..\src\ALaCarte.Cli\ALaCarte.Cli.csproj
set NUPKG_DIR=%~dp0..\..\artifacts

dotnet pack "%CLI_PROJECT%" -o "%NUPKG_DIR%"
dotnet tool install --global --add-source "%NUPKG_DIR%" QuinntyneBrown.ALaCarte.Cli
