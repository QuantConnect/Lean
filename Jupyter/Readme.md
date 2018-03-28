QuantConnect Jupyter Project:
=============
Before we enable Jupyter support, follow [Lean installation](https://github.com/QuantConnect/Lean#installation-instructions)
and [Python installation](https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#quantconnect-python-algorithm-project) to get LEAN running Python algorithms in your machine. 

### [Windows](https://github.com/QuantConnect/Lean#windows)
**1. Install Jupyter and its dependencies:**
   1. Install Jupyter
```
    pip install jupyter
```
 2.  Install [QuantConnect Python API](https://pypi.python.org/pypi/quantconnect/0.1)
 ```
    pip install quantconnect
```
 3.  Install pythonnet
 If you haven't yet, You should first set up [git](https://help.github.com/articles/set-up-git/). Then
 ```
    pip install --egg  git+https://github.com/QuantConnect/pythonnet
```
**2. Run Jupyter:**
   1. Update the config.json file in Lean/Jupyter/.
 ```
    "data-folder": "C:\\Users\\...\\Lean\\Data\\",
    "plugin-directory": "C:\\Users\\...\\Lean\\Launcher\\bin\\Debug",
 ```
   2. Rebuild the solution.
   3. In command line, 
```
    cd Lean/Jupyter/bin/Debug
    jupyter notebook
```
