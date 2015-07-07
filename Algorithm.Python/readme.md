QuantConnect Python Algorithm Project:
=============

To run algorithms in Lean your primary modules must be called "main.py".

WINDOWS:
1. Install Iron Python: http://ironpython.codeplex.com/releases/view/169382

2. Add an entry to the system path variable (C:\Program Files (x86)\IronPython 2.7)

3. Run the build script (build.bat).

If you install IronPython and add it to your path variable you can adjust the project settings to do the following steps automatically:
4. Right click on QuantConnect.Algorithm.Python project and click properties. On Build Events tab enter "build" in the post-build events.

5. Update your config file to point to the output file: "QuantConnect.Algorithm.Python.dll"


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

3. Run the build.sh, and point your config.json to the output "QuantConnect.Algorithm.Python.dll" file.