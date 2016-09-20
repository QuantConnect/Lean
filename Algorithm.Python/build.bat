REM QuantConnect Lean Engine -- Python Build Script.
REM Requires python algorithm to have a "main.py" which is the primary import point.

REM Set Python Compiler Location
SET pyc="C:\Program Files (x86)\IronPython 2.7\Tools\Scripts\pyc.py"
SET ipy="C:\Program Files (x86)\IronPython 2.7\ipy.exe"

REM Clean the output directory
del "QuantConnect.Algorithm.Python.dll"
del "../../../Launcher/bin/Debug/QuantConnect.Algorithm.Python.dll"
del "../../../Tests/bin/Debug/QuantConnect.Algorithm.Python.dll"

REM Collect the names of all files in current directory
setlocal EnableDelayedExpansion EnableExtensions
SET pyfiles=
for %%f in (..\..\*.py) do SET pyfiles=!pyfiles! %%f

REM IronPython Compile the Algorithmwith all the files in current directory
%ipy% %pyc% /target:dll /out:QuantConnect.Algorithm.Python !pyfiles!

REM Copy to the Lean Algorithm Project
echo f|xcopy /Y "QuantConnect.Algorithm.Python.dll" "../../../Launcher/bin/Debug/QuantConnect.Algorithm.Python.dll"
echo f|xcopy /Y "QuantConnect.Algorithm.Python.dll" "../../../Tests/bin/Debug/QuantConnect.Algorithm.Python.dll"