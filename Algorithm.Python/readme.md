QuantConnect Python Algorithm Project:
=============

Set [config](https://github.com/QuantConnect/Lean/blob/master/Launcher/config.json):
```json
"algorithm-type-name": "BasicTemplateAlgorithm",
"algorithm-language": "Python",
"algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
```

WINDOWS:
1. [Install Python 2.7](https://www.python.org/downloads/)

2. Add an entry to the system path variable (C:\Python27)

3. Change the extension to .dll of Lean\packages\QuantConnect.pythonnet._version_\build\Python.Runtime.win and move it to Lean\packages\QuantConnect.pythonnet._version_\lib.


LINUX:

To use Lean-Python on linux you'll need to install Python:
```
sudo apt-get install -y python-pip
```
Please checkout the command to install the whitelisted packages [here](https://github.com/QuantConnect/Lean/blob/master/DockerfileLeanFoundation#L19).