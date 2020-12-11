QuantConnect Python Algorithm Project:
=============

## Local Python Autocomplete
To enable autocomplete for your local Python IDE, install the `quantconnect-stubs` package from PyPI using the following command:
```
pip install quantconnect-stubs
```

To update your autocomplete to the latest version, you can run the following command:
```
pip install --upgrade quantconnect-stubs
```

Copy and paste the imports found [here](#python-autocomplete-imports) to the top of your project file to enable autocomplete.

In addition, you can use [Skylight](https://www.quantconnect.com/skylight) to automatically sync local changes to the cloud.

------

## Running LEAN Locally with Python
Before we enable python support, follow the [installation instructions](https://github.com/QuantConnect/Lean#installation-instructions) to get LEAN running C# algorithms in your machine. 

### Install Python 3.6:
#### [Windows](https://github.com/QuantConnect/Lean#windows)
1. Use the Windows x86-64 MSI **Python 3.6.8** installer from [python.org](https://www.python.org/downloads/release/python-368/) or [Anaconda](https://repo.anaconda.com/archive/Anaconda3-5.2.0-Windows-x86_64.exe) for Windows installer. "Anaconda 5.2" installs 3.5.2 by default, after installation of Anaconda you will need to upgrade python to make it work as expected: `conda install -y python=3.6.8`
2. When asked to select the features to be installed, make sure you select "Add python.exe to Path"
3. `[Optional]` Create `PYTHONHOME` system variables which value must be the location of your python installation (e.g. `C:\Python36amd64` or `C:\Anaconda3`):
   1. Right mouse button on My Computer. Click Properties.
   2. Click Advanced System Settings -> Environment Variables -> System Variables
   3. Click **New**. 
        - Name of the variable: `PYTHONHOME`. 
        - Value of the variable: python installation path.
4. Install [pandas=0.25.3](https://pandas.pydata.org/) and its [dependencies](https://pandas.pydata.org/pandas-docs/stable/install.html#dependencies).
5. Install [wrapt=1.11.2](https://pypi.org/project/wrapt/) module.
6. Reboot computer to ensure changes are propogated.

#### [macOS](https://github.com/QuantConnect/Lean#macos)

1. Use the macOS x86-64 package installer from [Anaconda](https://repo.anaconda.com/archive/Anaconda3-5.2.0-MacOSX-x86_64.pkg) and follow "[Installing on macOS](https://docs.anaconda.com/anaconda/install/mac-os)" instructions from Anaconda documentation page.
2. Install [pandas=0.25.3](https://pandas.pydata.org/) and its [dependencies](https://pandas.pydata.org/pandas-docs/stable/install.html#dependencies).
3. Install [wrapt=1.11.2](https://pypi.org/project/wrapt/) module.

*Note:* If you encounter the "System.DllNotFoundException: python3.6m" runtime error when running Python algorithms, or generating reports, on macOS:
1. Find `libpython3.6m.dylib` in your Python installation folder. If you installed Python with Anaconda, it may be found at
    ```
    /Users/{your_user_name}/anaconda3/lib/libpython3.6m.dylib
    ```
2. Open `Lean/Common/Python/Python.Runtime.dll.config`, add the following text under `<configuration> ... </configuration>` and save:
    ```
        <dllmap dll="python3.6m" target="{the path in step 1 including libpython3.6m.dylib}" os="osx"/>
    ```
Note: Specify the install of v3.6.8 _exactly_, i.e. if with conda `conda install python=3.6.8` as this is a known compatible version and other versions may have issues as of this writing. 

#### [Linux](https://github.com/QuantConnect/Lean#linux-debian-ubuntu)
By default, **miniconda** is installed in the users home directory (`$HOME`):
```
export PATH="$HOME/miniconda3/bin:$PATH"
wget https://cdn.quantconnect.com/miniconda/Miniconda3-4.5.12-Linux-x86_64.sh
bash Miniconda3-4.5.12-Linux-x86_64.sh -b
rm -rf Miniconda3-4.5.12-Linux-x86_64.sh
sudo ln -s $HOME/miniconda3/lib/libpython3.6m.so /usr/lib/libpython3.6m.so
conda update -y python conda pip
conda install -y cython=0.29.11
conda install -y pandas=0.25.3
conda install -y wrapt=1.11.2
```

*Note 1:* There is a [known issue](https://github.com/pythonnet/pythonnet/issues/609) with python 3.6.5 that prevents pythonnet installation, please upgrade python to version 3.6.8:
    ```
    conda install -y python=3.6.8
    ```
    
*Note 2:* If you encounter the "System.DllNotFoundException: python3.6m" runtime error when running Python algorithms on Linux:
1. Find `libpython3.6m.so` in your Python installation folder. If you installed Python with Miniconda, it may be found at
    ```
    /home/{your_user_name}/miniconda3/envs/{qc_environment}/lib/libpython3.6m.so
    ```
   Note that you can create a new virtual environment with all required dependencies by executing:
   ```
   conda create -n qc_environment python=3.6.8 cython=0.29.11 pandas=0.25.3 wrapt=1.11.2

   ```
2. Open `Lean/Common/Python/Python.Runtime.dll.config`, add the following text under `<configuration> ... </configuration>` and save:
    ```
        <dllmap dll="python3.6m" target="{the path in step 1 including libpython3.6m.so}" os="linux"/>
    ```
### Run python algorithm
1. Update the [config](https://github.com/QuantConnect/Lean/blob/master/Launcher/config.json) to run the python algorithm:
    ```json
    "algorithm-type-name": "BasicTemplateAlgorithm",
    "algorithm-language": "Python",
    "algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
    ```
 2. Rebuild LEAN.
 3. Run LEAN. You should see the same result of the C# algorithm you tested earlier.

___

### Python.NET development - Python.Runtime.dll compilation
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

# Python Autocomplete Imports
Copy and paste these imports to the top of your Python file to enable a development experience equal to the cloud (these imports are exactly the same as the ones used in the QuantConnect Terminal).

```python
from QuantConnect import *
from QuantConnect.Parameters import *
from QuantConnect.Benchmarks import *
from QuantConnect.Brokerages import *
from QuantConnect.Util import *
from QuantConnect.Interfaces import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Indicators import *
from QuantConnect.Data import *
from QuantConnect.Data.Consolidators import *
from QuantConnect.Data.Custom import *
from QuantConnect.Data.Fundamental import *
from QuantConnect.Data.Market import *
from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Notifications import *
from QuantConnect.Orders import *
from QuantConnect.Orders.Fees import *
from QuantConnect.Orders.Fills import *
from QuantConnect.Orders.Slippage import *
from QuantConnect.Scheduling import *
from QuantConnect.Securities import *
from QuantConnect.Securities.Equity import *
from QuantConnect.Securities.Forex import *
from QuantConnect.Securities.Interfaces import *
from datetime import date, datetime, timedelta
from QuantConnect.Python import *
from QuantConnect.Storage import *
QCAlgorithmFramework = QCAlgorithm
QCAlgorithmFrameworkBridge = QCAlgorithm
```

