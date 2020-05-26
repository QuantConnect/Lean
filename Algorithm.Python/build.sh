#!/bin/bash
mono nPython.exe -m compileall -lq .

cp *.pyc ../../../Launcher/bin/Debug/
cp *.pyc ../../../Launcher/bin/Release/
cp *.pyc ../../../Tests/bin/Debug/

# Script intentionally discards errors. The line below ensures the exit code is 0.
exit 0