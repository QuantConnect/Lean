QuantConnect Python Algorithm Project:
=============

Before we enable python support, follow the [installation instructions](https://github.com/QuantConnect/Lean#installation-instructions) to get LEAN running C# algorithms in your machine. 

### [Windows](https://github.com/QuantConnect/Lean#windows)
**1. Install Python 3.6:**
   1. Use the Windows x86-64 MSI installer from [https://www.python.org/downloads/release/python-364/](https://www.python.org/downloads/release/python-364/).
  2. When asked to select the features to be installed, make sure you select "Add python.exe to Path"
   3. Skip the next step if `python36.dll` can be found at `C:\Windows\System32`.
   4. Add `python36.dll` location to the system path:
      1. Right mouse button on My Computer. Click Properties.
      2. Click Advanced System Settings -> Environment Variables -> System Variables
      3. On the "Path" section click New and enter the `python36.dll` path, in our example this was `C:\Windows\System32`
      4. Create two new system variables: `PYTHONHOME` and `PYTHONPATH` which values must be, respectively, the location of your python installation (e.g. `C:\Python36amd64`) and its Lib folder (e.g. `C:\Python36amd64\Lib`).
 5. Install [pandas](https://pandas.pydata.org/) and its [dependencies](https://pandas.pydata.org/pandas-docs/stable/install.html#dependencies).

**2. Run python algorithm:**
   1. Prepare `Python.Runtime.dll`. This is needed to run Python algorithms in LEAN.
      1. Delete the existing files in `\Lean\packages\QuantConnect.pythonnet.1.0.5.7\lib`
      2. Using windows you'll need to copy the `\Lean\packages\QuantConnect.pythonnet.1.0.5.7\build\Python.Runtime.win` file into the `..\lib\` directory and rename it to `Python.Runtime.dll`.
  2. Update the [config](https://github.com/QuantConnect/Lean/blob/master/Launcher/config.json) to run the python algorithm:
```json
"algorithm-type-name": "BasicTemplateAlgorithm",
"algorithm-language": "Python",
"algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
```
 3. Rebuild LEAN. This step will ensure that the `Python.Runtime.dll` you set in 2.1 will be used.
      Note: Rebuilding will clean and build the whole solution from scratch, ignoring anything it's done before.
 4. Run LEAN. You should see the same result of the C# algorithm you tested earlier.

### [macOS](https://github.com/QuantConnect/Lean#macos)
**1. Install Python 3.6 with Anaconda:**
   1. Follow "[Installing on macOS](https://docs.anaconda.com/anaconda/install/mac-os)" instructions from Anaconda documentation page.
   2. Prepend the Anaconda install location to `DYLD_FALLBACK_LIBRARY_PATH`.
```
$ export DYLD_FALLBACK_LIBRARY_PATH="/<path to anaconda>/lib:$DYLD_FALLBACK_LIBRARY_PATH"
```
   3. For [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/) users: add symbolic links to python's dynamic-link libraries in `/usr/local/lib`
```
$ sudo mkdir /usr/local/lib
$ sudo ln -s /<path to anaconda>/lib/libpython* /usr/local/lib
```
   4. Install [pandas](https://pandas.pydata.org/) and its [dependencies](https://pandas.pydata.org/pandas-docs/stable/install.html#dependencies).

**2. Run python algorithm:**
   1. Prepare `Python.Runtime.dll`. This is needed to run Python algorithms in LEAN.
      1. Delete the existing files in `\Lean\packages\QuantConnect.pythonnet.1.0.5.7\lib`
      2. Using windows you'll need to copy the `\Lean\packages\QuantConnect.pythonnet.1.0.5.7\build\Python.Runtime.mac` file into the `..\lib\` directory and rename it to `Python.Runtime.dll`.
  2. Update the [config](https://github.com/QuantConnect/Lean/blob/master/Launcher/config.json) to run the python algorithm:
```json
"algorithm-type-name": "BasicTemplateAlgorithm",
"algorithm-language": "Python",
"algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
```
 3. Rebuild LEAN. This step will ensure that the `Python.Runtime.dll` you set in 2.1 will be used.
      Note: Rebuilding will clean and build the whole solution from scratch, ignoring anything it's done before.
 4. Run LEAN. You should see the same result of the C# algorithm you tested earlier.

### [Linux](https://github.com/QuantConnect/Lean#linux-debian-ubuntu)
**1. Install Python 3.6 with Miniconda:**
By default, **miniconda** is installed in the users home directory (`$HOME`):
```
export PATH="$HOME/miniconda3/bin:$PATH"
wget http://cdn.quantconnect.com.s3.amazonaws.com/miniconda/Miniconda3-4.3.31-Linux-x86_64.sh
bash Miniconda3-4.3.31-Linux-x86_64.sh -b
rm -rf Miniconda3-4.3.31-Linux-x86_64.sh
sudo ln -s $HOME/miniconda3/lib/libpython3.6m.so /usr/lib/libpython3.6.so
conda update -y python conda pip
conda install -y cython pandas
```
**2 Run python algorithm:**
 1. Update the [config](https://github.com/QuantConnect/Lean/blob/master/Launcher/config.json) to run the python algorithm:
```json
"algorithm-type-name": "BasicTemplateAlgorithm",
"algorithm-language": "Python",
"algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
```
 2. Run LEAN. You should see the same result of the C# algorithm you tested earlier.
___
#### Python.Runtime.dll compilation
LEAN users do **not** need to compile `Python.Runtime.dll`. The information below is targeted to developers who wish to improve it. 

Download [QuantConnect/pythonnet](https://github.com/QuantConnect/pythonnet/) github clone or downloading the zip. If downloading the zip - unzip to a local pathway.

**Note:** QuantConnect's version of pythonnet is an enhanced version of [pythonnet](https://github.com/pythonnet/pythonnet) with added support for `System.Decimal` and `System.DateTime`.

Below we can find the compilation flags that create a suitable `Python.Runtime.dll` for each operating system.

**Windows**
```
msbuild pythonnet.sln /nologo /v:quiet /t:Clean;Rebuild /p:Platform=x64 /p:PythonInteropFile="interop36.cs" /p:Configuration=ReleaseWin /p:DefineConstants="PYTHON36,PYTHON3,UCS2"
```
**macOS**
```
msbuild pythonnet.sln /nologo /v:quiet /t:Clean;Rebuild /p:Platform=x64 /p:PythonInteropFile="interop36m.cs" /p:Configuration=ReleaseMono /p:DefineConstants="PYTHON36,PYTHON3,UCS4,MONO_OSX,PYTHON_WITH_PYMALLOC"
```
**Linux**
```
msbuild pythonnet.sln /nologo /v:quiet /t:Clean;Rebuild /p:Platform=x64 /p:PythonInteropFile="interop36m.cs" /p:Configuration=ReleaseMono /p:DefineConstants="PYTHON36,PYTHON3,UCS4,MONO_LINUX,PYTHON_WITH_PYMALLOC"
```