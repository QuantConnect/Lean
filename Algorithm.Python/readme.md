QuantConnect Python Algorithm Project:
=============

Before we enable python support, follow the [installation instructions](https://github.com/QuantConnect/Lean#installation-instructions) to get LEAN running C# algorithms in your machine. 

### [Windows](https://github.com/QuantConnect/Lean#windows)
**1. Install Python 3.6:**
   1. Use the Windows x86-64 MSI installer from [https://www.python.org/downloads/release/python-364/](https://www.python.org/downloads/release/python-364/).
  2. When asked to select the features to be installed, make sure you select "Add python.exe to Path"
   3. Once installed confirm the python36.dll is in place in *C:\Windows\System32*.
   4. Add python36.dll location to the system path:
      1. Right mouse button on My Computer. Click Properties.
      2. Click Advanced System Settings -> Environment Variables -> System Variables
      3. On the "Path" section click New and enter the python36.dll path, in our example this was *C:\Windows\System32*
      4. Create two new system variables: PYTHONHOME and PYTHONPATH which values must be, respectively, the location of your python installation (e.g. *C:\Python36amd64*) and its Lib folder (e.g. *C:\Python36amd64\Lib*).
      5. *C:\Windows\System32* is normally in the path, thus this procedure is not necessary.
 5. Install [pandas](https://pandas.pydata.org/) and its [dependencies](https://pandas.pydata.org/pandas-docs/stable/install.html#dependencies).

**2. Run python algorithm:**
   1. Prepare Python.Runtime.dll. This is needed to run Python algorithms in LEAN.
      1. Delete the existing files in \Lean\packages\QuantConnect.pythonnet.1.0.5.5\lib
      2. Using windows you'll need to copy the \Lean\packages\QuantConnect.pythonnet.1.0.5.5\build\ *Python.Runtime.win* file into the ..\lib\ directory and rename it to Python.Runtime.dll.
  2. Update the [config](https://github.com/QuantConnect/Lean/blob/master/Launcher/config.json) to run the python algorithm:
```json
"algorithm-type-name": "BasicTemplateAlgorithm",
"algorithm-language": "Python",
"algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
```
 3. Rebuild LEAN. This step will ensure that the Python.Runtime.dll you set in 2.1 will be used.
 4. Run LEAN. You should see the same result of the C# algorithm you tested earlier.

### [macOS](https://github.com/QuantConnect/Lean#macos)
**1. Install Python 3.6:**
 1. By default, macOS has python installed.
 2. Confirm the libpython3.6.dylib is in place in */usr/lib*.
 3. If libpython3.6.dylib is not in */usr/lib*, find and link it into */usr/lib*:
```
$ find / -name libpython3.6.dylib
/System/Library/Frameworks/Python.framework/Versions/3.6/lib/libpython3.6.dylib
$ ln -s /System/Library/Frameworks/Python.framework/Versions/3.6/lib/libpython3.6.dylib /usr/lib/libpython3.6.dylib
```
  - Common Mistake: Often you may get a permissions error when trying to setup this sym link - you should check if the file already exists in the */usr/lib* directory. If its there you don't need to add the sym link.
 4. If libpython3.6.dylib cannot be found, reinstall python.
 5. Install [pandas](https://pandas.pydata.org/) and its [dependencies](https://pandas.pydata.org/pandas-docs/stable/install.html#dependencies)
    1. If you want to use pip to install new packages and it is not available, you can install it with the command
```
$ sudo easy-install pip
```

**2. Run python algorithm:**
 1. Prepare Python.Runtime.dll. This is needed to run Python algorithms in LEAN.
    1. Delete the existing files in \Lean\packages\QuantConnect.pythonnet.1.0.5.5\lib
    2. Using macOS you'll need to copy the \Lean\packages\QuantConnect.pythonnet.1.0.5.5\build\ *Python.Runtime.mac* file into the ..\lib\ directory and rename it to Python.Runtime.dll.
 2. Update the [config](https://github.com/QuantConnect/Lean/blob/master/Launcher/config.json) to run the python algorithm:
```json
"algorithm-type-name": "BasicTemplateAlgorithm",
"algorithm-language": "Python",
"algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
```
 3. Rebuild LEAN. This step will ensure that the Python.Runtime.dll you set in 2.1 will be used.
 4. Run LEAN. You should see the same result of the C# algorithm you tested earlier.
 5. If there is an issue with the dll, please use *Python.Runtime.mac4* in 2.1.2 and rebuild the solution. 

### [Linux](https://github.com/QuantConnect/Lean#linux-debian-ubuntu)
**1. Install Python 3.6 with Miniconda:**
```
export PATH="/opt/miniconda3/bin:$PATH"
wget http://cdn.quantconnect.com.s3.amazonaws.com/miniconda/Miniconda3-4.3.31-Linux-x86_64.sh
bash Miniconda3-4.3.31-Linux-x86_64.sh -b
rm -rf Miniconda3-4.3.31-Linux-x86_64.sh
ln -s /opt/miniconda3/lib/libpython3.6m.so /usr/lib/libpython3.6.so
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
LEAN users do **not** need to compile Python.Runtime.dll. The information below is targeted to developers who wish to improve it. 

Download [QuantConnect/pythonnet](https://github.com/QuantConnect/pythonnet/) github clone or downloading the zip. If downloading the zip - unzip to a local pathway.

**Note:** QuantConnect's version of pythonnet is an enhanced of [pythonnet](https://github.com/pythonnet/pythonnet) with support to System.Decimal and System.DateTime.

Below we can find the compilation flags that create a suitable Python.Runtime.dll for each operating system.

**Windows**
```
msbuild pythonnet.sln /nologo /v:quiet /t:Clean;Rebuild /p:Platform=x64 /p:PythonInteropFile="interop36.cs" /p:Configuration=ReleaseWin /p:DefineConstants="PYTHON36,PYTHON3,UCS2"
```

**macOS UCS2**
```
msbuild pythonnet.sln /nologo /v:quiet /t:Clean;Rebuild /p:Platform=x64 /p:PythonInteropFile="interop36.cs" /p:Configuration=ReleaseMono /p:DefineConstants="PYTHON36,PYTHON3,UCS2,MONO_OSX"
```

**macOS UCS4**
```
msbuild pythonnet.sln /nologo /v:quiet /t:Clean;Rebuild /p:Platform=x64 /p:PythonInteropFile="interop36.cs" /p:Configuration=ReleaseMono /p:DefineConstants="PYTHON36,PYTHON3,UCS4,MONO_OSX"
```


**Linux**
```
msbuild pythonnet.sln /nologo /v:quiet /t:Clean;Rebuild /p:Platform=x64 /p:PythonInteropFile="interop36.cs" /p:Configuration=ReleaseMono /p:DefineConstants="PYTHON36,PYTHON3,UCS4,MONO_LINUX"
```