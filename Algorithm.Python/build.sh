# QuantConnect Lean Algorithmic Trading Engine: Python Builder Script

# Clean output directory
rm QuantConnect.Algorithm.Python.dll

# Set the script variables: assuming installing the ./compiler/library/ and the caller is in ./compiler/bin/Debug/build.sh
ipy="../../IronPython-2.7.5/ipy.exe"
pyc="../../IronPython-2.7.5/Tools/Scripts/pyc.py"
 
# Call the compiler:
mono $ipy $pyc /target:dll /out:QuantConnect.Algorithm.Python main.py