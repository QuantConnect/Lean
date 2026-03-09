REM Python Compile the Algorithm with all the files in current directory
nPython -m compileall -lq ..\..\

REM Copy to the Lean Algorithm Project
copy ..\..\__pycache__\*.pyc ..\..\..\Launcher\bin\Debug >NUL
copy ..\..\__pycache__\*.pyc ..\..\..\Launcher\bin\Release >NUL
copy ..\..\__pycache__\*.pyc ..\..\..\Tests\bin\Debug >NUL

REM Script intentionally discards errors. This line ensures the exit code is 0.