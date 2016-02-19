# QuantConnect Lean Algorithmic Trading Engine: Python Builder Script

# Clean output directory
rm QuantConnect.Algorithm.Python.dll
rm ../Launcher/bin/Debug/QuantConnect.Algorithm.Python.dll
rm ../Tests/bin/Debug/QuantConnect.Algorithm.Python.dll

# Set the script variables: assuming installing the ./compiler/library/ and the caller is in ./compiler/bin/Debug/build.sh
ipy="../../IronPython-2.7.5/ipy.exe"
pyc="../../IronPython-2.7.5/Tools/Scripts/pyc.py"

# Call the compiler with all the files in current directory
echo mono $ipy $pyc /target:dll /out:QuantConnect.Algorithm.Python $(ls *.py)

# Copy to the Lean Algorithm Project
cp QuantConnect.Algorithm.Python.dll ../Launcher/bin/Debug/QuantConnect.Algorithm.Python.dll
cp QuantConnect.Algorithm.Python.dll ../Tests/bin/Debug/QuantConnect.Algorithm.Python.dll