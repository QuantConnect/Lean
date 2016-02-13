REM QuantConnect Lean Engine -- Python Build Script.
REM Requires python algorithm to have a "main.py" which is the primary import point.

REM Set Python Compiler Location
SET pyc="C:\Program Files (x86)\IronPython 2.7\Tools\Scripts\pyc.py"

REM Clean the output directory
del "QuantConnect.Algorithm.Python.dll"
del "../Launcher/bin/Debug/QuantConnect.Algorithm.Python.dll"

REM Get algorithm-type-name from config.json
setlocal EnableDelayedExpansion
for /f "tokens=2 delims=:, " %%a in (' find "algorithm-type-name" ^< "../Launcher/config.json" ') do ( set pyfile=%%~a.py )

REM IronPython Compile the Algorithm
ipy %pyc% /target:dll /out:QuantConnect.Algorithm.Python !pyfile!

REM Copy to the Lean Algorithm Project
echo f|xcopy /Y "QuantConnect.Algorithm.Python.dll" "../Launcher/bin/Debug/QuantConnect.Algorithm.Python.dll"