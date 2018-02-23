QuantConnect Python Algorithm Project:
=============

### Windows
**1. Install Visual Studio:**

1.1 Visual Studio 2017 Community edition can be downloaded free from [https://www.visualstudio.com](https://www.visualstudio.com). VS comes with Python (if you install Python Tools for Visual Studio) but it doesn't seem to add Python to the system path. We'll need to reinstall python manually. If you already have VS installed you should be able to skip this step.

**2. Install Python 3.6:**

2.1 Use the Windows x86-64 MSI installer from [https://www.python.org/downloads/release/python-364/](https://www.python.org/downloads/release/python-364/).

2.2 When asked to select the features to be installed, make sure you select "Add python.exe to Path"

2.3 Once installed confirm the python36.dll is in place in *C:\Windows\System32*.

2.4 Add python36.dll location to the system path:
 
2.4.1 Right mouse button on My Computer. Click Properties.

2.4.2 Click Advanced System Settings -> Environment Variables -> System Variables

2.4.3 On the "Path" section click New and enter the python36.dll path, in our example this was *C:\Windows\System32*

2.4.4 *C:\Windows\System32* is normally in the path, thus this procedure is not necessary.

2.5 Install [pandas](https://pandas.pydata.org/) and its [dependencies](https://pandas.pydata.org/pandas-docs/stable/install.html#dependencies).

**3. Setup LEAN:**

3.1 Download LEAN github clone or downloading the zip. If downloading the zip - unzip to a local pathway.

3.2 Prepare Python.Runtime.dll. This is needed to run Python algorithms in LEAN.

3.2.1 Delete the existing files in \Lean\packages\QuantConnect.pythonnet.1.0.5.5\lib

3.2.2 Using windows you'll need to copy the \Lean\packages\QuantConnect.pythonnet.1.0.5.5\build\*Python.Runtime.win* file into the ..\lib\ directory and rename it to Python.Runtime.dll.

**4 Run LEAN:**

4.1 Run with a basic C# algorithm first to make sure it works (using the default credentials, just press F5).

4.1.1 You should see that it exits successfully with a list of the algorithm statistics and 1 trade.

4.2 Update the [config](https://github.com/QuantConnect/Lean/blob/master/Launcher/config.json) to run the python algorithm:

```json
"algorithm-type-name": "BasicTemplateAlgorithm",
"algorithm-language": "Python",
"algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
```

4.2.1 You should see the same result of 4.1.1. 


### macOS
**1. Install Visual Studio:**

1.1 Visual Studio 2017 Community edition can be downloaded free from [https://www.visualstudio.com/visual-studio-mac](https://www.visualstudio.com/visual-studio-mac). VS will install Mono that is a requirement for running Windows executables like Lean. If you already have VS and/or Mono installed you should be able to skip this step.

**2. Install Python 3.6:**

2.1 By default, macOS has python installed.

2.2 Confirm the libpython3.6.dylib is in place in */usr/lib*.

2.3 If libpython3.6.dylib is not in */usr/lib*, find and link it into */usr/lib*:
```
$ find / -name libpython3.6.dylib
/System/Library/Frameworks/Python.framework/Versions/3.6/lib/libpython3.6.dylib
$ ln -s /System/Library/Frameworks/Python.framework/Versions/3.6/lib/libpython3.6.dylib /usr/lib/libpython3.6.dylib
```

2.3.1 Common Mistake: Often you may get a permissions error when trying to setup this sym link - you should check if the file already exists in the */usr/lib* directory. If its there you don't need to add the sym link.

2.4 If libpython3.6.dylib cannot be found, reinstall python.

2.5 Install [pandas](https://pandas.pydata.org/) and its [dependencies](https://pandas.pydata.org/pandas-docs/stable/install.html#dependencies)

2.5.1 If you want to use pip to install new packages and it is not available, you can install it with the command
```
$ sudo easy-install pip
```

**3. Setup LEAN:**

3.1 Download LEAN github clone, or downloading the zip. If downloading the zip - unzip to a local pathway.

3.2 Prepare Python.Runtime.dll. This is needed to run Python algorithms in LEAN.

3.2.1 Delete the existing files in \Lean\packages\QuantConnect.pythonnet.1.0.5.5\lib

3.2.2 Using macOS you'll need to copy the \Lean\packages\QuantConnect.pythonnet.1.0.5.5\build\*Python.Runtime.mac* file into the ..\lib\ directory and rename it to Python.Runtime.dll.

**4 Run LEAN:**

4.1 Run with a basic C# algorithm first to make sure it works.

4.1.1 You should see that it exits successfully with a list of the algorithm statistics and 1 trade.

4.2 Update the [config](https://github.com/QuantConnect/Lean/blob/master/Launcher/config.json) to run the python algorithm:
```json
"algorithm-type-name": "BasicTemplateAlgorithm",
"algorithm-language": "Python",
"algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
```

4.2.1 You should see the same result of 4.1.1.

4.2.2 If there is an issue with the dll, please use *Python.Runtime.mac4* in 3.2.2 and rebuild the solution. 


### Linux
**1. Follow the installation instructions [here](https://github.com/QuantConnect/Lean#linux-debian-ubuntu)**

**2. Install Python 3.6 with Miniconda:**
```
export PATH="/root/miniconda3/bin:$PATH"
wget https://repo.continuum.io/miniconda/Miniconda3-latest-Linux-x86_64.sh
bash Miniconda3-latest-Linux-x86_64.sh -b
rm -rf Miniconda3-latest-Linux-x86_64.sh
ln -s /root/miniconda3/lib/libpython3.6m.so /usr/lib/libpython3.6.so
conda update -y python conda pip
conda install -y cython pandas
```

**3 Run LEAN:**

3.1 You should see this exit successfully with a list of the algorithm statistics and 1 trade.

3.2 Update the [config](https://github.com/QuantConnect/Lean/blob/master/Launcher/config.json) to run the python algorithm:
```json
"algorithm-type-name": "BasicTemplateAlgorithm",
"algorithm-language": "Python",
"algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
```
1.2.1 You should see the same result of 3.1.

___
#### Python.Runtime.dll compilation
Lean users do **not** need to compile Python.Runtime.dll. The information below is targeted to developers who wish to improve it. 

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
