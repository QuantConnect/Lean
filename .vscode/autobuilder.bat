: Move to Launcher directory
set CURRENT_DIR=%~dp0
cd %CURRENT_DIR%../Launcher

: Enable dotnet watch to trigger builds on file change
dotnet watch build --configuration Debug