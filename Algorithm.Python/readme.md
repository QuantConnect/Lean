QuantConnect Python Algorithm Project:
=============

To run algorithms in Lean your primary modules must be called "main.py".

Set your config "algorithm-language" property to "Python".
Set your config "algorithm-location" property to "QuantConnect.Algorithm.Python.dll".

WINDOWS:
1. Install Iron Python: http://ironpython.codeplex.com/releases/view/169382

2. Add an entry to the system path variable (C:\Program Files (x86)\IronPython 2.7)

3. Run the build script (build.bat).

4. Optional: Right click on QuantConnect.Algorithm.Python project and click properties. On Build Events tab enter "build" in the post-build events.

LINUX:
To use Lean-Python on linux you'll need to install IronPython as well as Python. This is based from this guide:
http://ironpython.codeplex.com/wikipage?title=IronPython%20on%20Mono

1. Install Python(2.7): sudo apt-get install python-all zip git make

2. Install Iron Python 2.7.5:
   - Go to this website and downlaod to a local directory:
   - wget https://github.com/IronLanguages/main/releases/download/ipy-2.7.5/IronPython-2.7.5.zip

   - extract: to a base directory or your local bin.
     unzip IronPython-2.7.5.zip 
	
   - Update the build.sh variable "pyc" to point towards your pyc.py file, it should be under ./IronPython-2.7.5/Tools/Scripts/pyc.py

3. Run the build.sh.
