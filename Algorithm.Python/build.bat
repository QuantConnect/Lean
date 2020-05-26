REM QuantConnect Lean Engine -- Python Build Script.
del Python.Runtime.dll

REM Python Compile the Algorithm with all the files in current directory
nPython -m compileall -lq .

REM Copy to the Lean Algorithm Project
copy *.pyc ..\..\..\Launcher\bin\Debug >NUL
copy *.pyc ..\..\..\Launcher\bin\Release >NUL
copy *.pyc ..\..\..\Tests\bin\Debug >NUL

REM Script intentionally discards errors. This line ensures the exit code is 0.