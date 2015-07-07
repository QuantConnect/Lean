REM QuantConnect Lean Engine -- Python Build Script.
REM Requires python algorithm to have a "main.py" which is the primary import point.

REM Set Python Compiler Location
SET pyc="C:\Program Files (x86)\IronPython 2.7\Tools\Scripts\pyc.py"

REM Clean the output directory
del "QuantConnect.Algorithm.Python.dll"
del "../../../Engine/bin/Debug/QuantConnect.Algorithm.Python.dll"

REM IronPython Compile the Algorithm
ipy %pyc% /target:dll /out:QuantConnect.Algorithm.Python main.py

REM Copy to the Lean Algorithm Project
echo f|xcopy /Y "QuantConnect.Algorithm.Python.dll" "../../../Engine/bin/Debug/QuantConnect.Algorithm.Python.dll"