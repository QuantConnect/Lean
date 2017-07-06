QuantConnect Python Algorithm Project:
=============

Set [config](https://github.com/QuantConnect/Lean/blob/master/Launcher/config.json):
```json
"algorithm-type-name": "BasicTemplateAlgorithm",
"algorithm-language": "Python",
"algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
```

### Linux
To use Lean-Python on linux you'll need to install Python:
```
sudo apt-get install -y python-pip
```
Please checkout the command to install the whitelisted packages [here](https://github.com/QuantConnect/Lean/blob/master/DockerfileLeanFoundation#L19).

### Windows
1. [Install Python 2.7](https://www.python.org/downloads/)

2. Add an entry to the system path variable (C:\Python27)

3. Rename `Lean\packages\QuantConnect.pythonnet._version_\build\Python.Runtime.win` to `Lean\packages\QuantConnect.pythonnet._version_\lib\Python.Runtime.dll`

### macOS
By default, macOS has python installed.

Rename `Lean\packages\QuantConnect.pythonnet._version_\build\Python.Runtime.mac` to `Lean\packages\QuantConnect.pythonnet._version_\lib\Python.Runtime.dll`

### pandas
Install [pandas](https://pandas.pydata.org/) and its [dependencies](https://pandas.pydata.org/pandas-docs/stable/install.html#dependencies).
