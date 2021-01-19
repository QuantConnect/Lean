#!/bin/bash
cp ../Launcher/config.json ./bin/Debug >  /dev/null
cp ../Launcher/config.json ./bin/Release > /dev/null

# Script intentionally discards errors. The line below ensures the exit code is 0.
exit 0
