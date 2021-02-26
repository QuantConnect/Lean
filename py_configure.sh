#!/bin/bash

# Help message
if [ "$1" == --help ]; then
  printf "Lean Python Configuration Utility
---------------------------------
Usage: bash py_configure.sh [location]
Arguments:
       location --  Path to libpython3.6m.so (Linux) or libpython3.6m.dylib (OSX)
                    Note: if not specified, this script will attempt to find it within your environment.
"
  exit 0
elif [ -n "$1" ]; then
  PY_FLAGS="$1"
  PY_PREF="$PY_FLAGS"
  if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    MAP_CONF="<dllmap os=\"linux\" dll=\"python3.6m\" target=\"$PY_PREF\"/>"
  elif [[ "$OSTYPE" == "darwin"* ]]; then
    MAP_CONF="<dllmap os=\"osx\" dll=\"python3.6m\" target=\"$PY_PREF\"/>"
  fi
else
  # Check for correct python version
  PY_FLAGS=$(python3-config --includes)
  PY_PREF=$(python3-config --prefix)
  if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    MAP_CONF="<dllmap os=\"linux\" dll=\"python3.6m\" target=\"$PY_PREF/lib/libpython3.6m.so\"/>"
  elif [[ "$OSTYPE" == "darwin"* ]]; then
    MAP_CONF="<dllmap os=\"osx\" dll=\"python3.6m\" target=\"$PY_PREF\/lib/libpython3.6m.dylib/>"
  fi
fi

# Check if valid
PY_FLAGS=$(echo "$PY_FLAGS" | awk '{print $1;}')
if [[ "$PY_FLAGS" != *"python3.6m"* ]]; then
  echo "ERROR: Python 3.6 must be configured as the environment interpreter or be specified!"
  exit 1
fi
echo "Valid Python 3.6 environment located: $PY_PREF"

# If no error, write configuration
echo "Writing Python configuration to Common/Python/Python.Runtime.dll.config"
rm Common/Python/Python.Runtime.dll.config

cat <<EOF >>Common/Python/Python.Runtime.dll.config
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    $MAP_CONF
</configuration>
EOF
exit 0
