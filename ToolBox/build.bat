REM Copies the config.json file to the output directory
copy ..\Launcher\config.json .\bin\Debug > NUL
copy ..\Launcher\config.json .\bin\Release > NUL

REM Script intentionally discards errors. This line ensures the exit code is 0.
