#!/bin/bash
mono nPython.exe -m compileall -lq ../../

cp ../../__pycache__/*.pyc ../../../Launcher/bin/Debug > /dev/null
cp ../../__pycache__/*.pyc ../../../Launcher/bin/Release > /dev/null
cp ../../__pycache__/*.pyc ../../../Tests/bin/Debug > /dev/null

# Script intentionally discards errors. The line below ensures the exit code is 0.
exit 0