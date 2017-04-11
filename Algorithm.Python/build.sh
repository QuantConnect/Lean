#!/bin/bash
mono nPython.exe -m compileall -lq .

cp *.pyc ../../../Launcher/bin/Debug/
cp *.pyc ../../../Launcher/bin/Release/
cp *.pyc ../../../Tests/bin/Debug/
