QuantConnect Jupyter Project:
=============
Before we enable Jupyter support, follow [Lean installation](https://github.com/QuantConnect/Lean#installation-instructions)
and [Python installation](https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#quantconnect-python-algorithm-project) to get LEAN running Python algorithms in your machine. 

### [Windows](https://github.com/QuantConnect/Lean#windows)
**1. Install Jupyter and its dependencies:**
   1. Install Jupyter:
```
    pip install jupyter
```
 2.  Install [QuantConnect Python API](https://pypi.python.org/pypi/quantconnect/0.1)
 ```
    pip install quantconnect
```
**2. Run Jupyter:**
   1. Update the config.json file in `Lean/Launcher/` folder
 ```
    "composer-dll-directory": ".",
 ```
   2. Rebuild the solution to refresh the configuration and binaries.
   3. Run Jupyter from the command line, 
```
    cd Lean/Launcher/bin/Debug
    jupyter lab
```