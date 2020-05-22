
QuantConnect Research Project:
=============
Before we enable Jupyter support, follow [Lean installation](https://github.com/QuantConnect/Lean#installation-instructions)
and [Python installation](https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#quantconnect-python-algorithm-project) to get LEAN running Python algorithms in your machine. 

**1. Installation:**
   1. Install [JupyterLab](https://pypi.org/project/jupyterlab/):
```
    pip install jupyterlab
```
 2.  Install [QuantConnect Python API](https://pypi.python.org/pypi/quantconnect/0.1)
 ```
    pip install quantconnect
```
 3. **Linux and macOS:** Copy pythonnet binaries for jupyter
 ```
  cp Lean/Launcher/bin/Debug/jupyter/* Lean/Launcher/bin/Debug
 ```
**2. Run Jupyter:**
   1. Update the `config.json` file in `Lean/Launcher/bin/Debug/` folder
 ```
    "composer-dll-directory": ".",
 ```
   2. Run Jupyter from the command line
```
    cd Lean/Launcher/bin/Debug
    jupyter lab
```