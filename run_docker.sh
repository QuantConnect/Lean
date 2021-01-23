#!/usr/bin/env bash
# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# allow script to be called from anywhere
cd "$(dirname "$0")/" || exit

DEFAULT_IMAGE=quantconnect/lean:latest
DEFAULT_DATA_DIR=./Data
DEFAULT_RESULTS_DIR=./Results
DEFAULT_CONFIG=./Launcher/config.json
DEFAULT_PYTHON_DIR=./Algorithm.Python/
CSHARP_DLL=./Launcher/bin/Debug/QuantConnect.Algorithm.CSharp.dll
CSHARP_PDB=./Launcher/bin/Debug/QuantConnect.Algorithm.CSharp.pdb
CONTAINER_NAME=LeanEngine
DEFAULT_TERM=Debug
DEFAULT_EXCLUDE=.git,.vs,.idea

yes_or_no() {
  while true; do
    read -p "$* [y/n]: " yn
    case $yn in
    [Yy]*) return 0 ;;
    [Nn]*)
      echo "Aborted"
      return 1
      ;;
    esac
  done
}

# Realpath polyfill, notably absent macOS and some debian distros
absolute_path() {
  echo "$(cd "$(dirname "${1}")" && pwd)/$(basename "${1}")"
}

# TODO: Add more details. Functions mostly as a placeholder to avoid immediate execution
if [[ "$1" =~ ^("-h"|"--help") ]]; then
  echo "Lean Engine -- Docker Runner"
  echo ""
  echo "Usage: bash run_docker.sh [OPTION]"
  exit 1
fi
# If arg is a file process the key values
if [ -f "$1" ]; then
  IFS="="
  while read -r key value; do
    eval "$key='$value'"
  done <"$1"
# If there are in line args, process them
elif [ -n "$*" ]; then
  for arg in "$@"; do
    eval "$arg"
  done
# Else query user for settings
else
  read -p "Would you like to debug a script [Debug] or launch into a terminal? [Term] [default: $DEFAULT_TERM]: " TERM_CHECK
  TERM_CHECK=${TERM_CHECK:-$DEFAULT_TERM}
  if [ "$DEFAULT_TERM" == "$TERM_CHECK" ]; then
    read -p "Docker image [default: $DEFAULT_IMAGE]: " IMAGE
    read -p "Path to Lean config.json [default: $DEFAULT_CONFIG]: " CONFIG_FILE
    read -p "Path to Data directory [default: $DEFAULT_DATA_DIR]: " DATA_DIR
    read -p "Path to Results directory [default: $DEFAULT_RESULTS_DIR]: " RESULTS_DIR
    read -p "Path to Python directory [default: $DEFAULT_PYTHON_DIR]: " PYTHON_DIR
    read -p "Would you like to debug C#? (Requires mono debugger attachment) [default: N]: " DEBUGGING
    read -p "Would you like to update the Docker Image? [default: Y]: " UPDATE
  else
    read -p "Should this directory be mounted? [default N]: " MOUNT
    if [ "$MOUNT" != "Y" ]; then
      echo "Which directories should be excluded?"
      read -p "Specify multiple directories as: Dir1,Dir2,Dir3, or write None for none [default: $DEFAULT_EXCLUDE]: " EXCLUDE
    fi
  fi
fi

# Check for existing LeanEngine
if [ "$($SUDO docker container inspect -f '{{.State.Running}}' $CONTAINER_NAME)" == "true" ]; then
  yes_or_no "A Lean container is already running. Stop and recreate with this configuration?" &&
    $SUDO docker stop $CONTAINER_NAME
elif $SUDO docker ps -a | grep -q $CONTAINER_NAME; then
  yes_or_no "A Lean container is halted and will be removed. Continue?" &&
    $SUDO docker rm $CONTAINER_NAME
fi

# Pull the image if we want to update
if [[ "$UPDATE" == "y" ]]; then
  echo "Pulling Docker image: $IMAGE"
  $SUDO docker pull "$IMAGE"
fi

if [ "$TERM_CHECK" == "$DEFAULT_TERM" ]; then
  # Have to reset IFS for cfg files to work properly
  IFS=" "

  # Fall back to defaults on empty input without
  CONFIG_FILE=${CONFIG_FILE:-$DEFAULT_CONFIG}
  DATA_DIR=${DATA_DIR:-$DEFAULT_DATA_DIR}
  RESULTS_DIR=${RESULTS_DIR:-$DEFAULT_RESULTS_DIR}
  IMAGE=${IMAGE:-$DEFAULT_IMAGE}
  PYTHON_DIR=${PYTHON_DIR:-$DEFAULT_PYTHON_DIR}
  UPDATE=${UPDATE:-Y}

  # Convert to absolute paths
  CONFIG_FILE=$(absolute_path "${CONFIG_FILE}")
  PYTHON_DIR=$(absolute_path "${PYTHON_DIR}")
  DATA_DIR=$(absolute_path "${DATA_DIR}")
  RESULTS_DIR=$(absolute_path "${RESULTS_DIR}")
  CSHARP_DLL=$(absolute_path "${CSHARP_DLL}")
  CSHARP_PDB=$(absolute_path "${CSHARP_PDB}")

  if [ ! -f "$CONFIG_FILE" ]; then
    echo "Lean config file $CONFIG_FILE does not exist"
    exit 1
  fi

  if [ ! -d "$DATA_DIR" ]; then
    echo "Data directory $DATA_DIR does not exist"
    exit 1
  fi

  if [ ! -d "$RESULTS_DIR" ]; then
    echo "Results directory $RESULTS_DIR does not exist; creating it now"
    mkdir $RESULTS_DIR
  fi

  # First part of the docker DOCKER_CMD that is static, then we build the rest
  DOCKER_CMD="docker run --rm \
      --mount type=bind,source=$CONFIG_FILE,target=/Lean/Launcher/config.json,readonly \
      -v $DATA_DIR:/Data:ro \
      -v $RESULTS_DIR:/Results \
      --name $CONTAINER_NAME \
      -p 5678:5678 \
      --expose 6000 "

  if [[ "$(uname)" == "Linux" ]]; then
    DOCKER_CMD+="--add-host=host.docker.internal:$(ip -4 addr show docker0 | grep -Po 'inet \K[\d.]+') "
  fi

  # If the csharp dll and pdb are present, mount them
  if [ ! -f "$CSHARP_DLL" ]; then
    echo "Csharp file at '$CSHARP_DLL' does not exist; no CSharp files will be mounted"
  else
    DOCKER_CMD+="--mount type=bind,source=$CSHARP_DLL,target=/Lean/Launcher/bin/Debug/QuantConnect.Algorithm.CSharp.dll \
      --mount type=bind,source=$CSHARP_PDB,target=/Lean/Launcher/bin/Debug/QuantConnect.Algorithm.CSharp.pdb "
  fi

  # If python algorithms are present, mount them
  if [ ! -d "$PYTHON_DIR" ]; then
    echo "No Python Algorithm location found at '$PYTHON_DIR'; no Python files will be mounted"
  else
    DOCKER_CMD+="-v $PYTHON_DIR:/Lean/Algorithm.Python "
  fi

  # If DEBUGGING is set then set the entrypoint to run mono with a debugger server
  shopt -s nocasematch
  if [[ "$DEBUGGING" == "y" ]]; then
    DOCKER_CMD+="-p 55555:55555 \
      --entrypoint mono \
      $IMAGE --debug --debugger-agent=transport=dt_socket,server=y,address=0.0.0.0:55555,suspend=y \
      QuantConnect.Lean.Launcher.exe --data-folder /Data --results-destination-folder /Results --config /Lean/Launcher/config.json"

    echo "Docker container starting, attach to Mono process at localhost:55555 to begin"
  else
    DOCKER_CMD+="$IMAGE --data-folder /Data --results-destination-folder /Results --config /Lean/Launcher/config.json"
  fi

  SUDO=""

  # Verify if user has docker permissions
  if ! touch /var/run/docker.sock &>/dev/null; then
    sudo -v
    SUDO="sudo"
    DOCKER_CMD="$SUDO $DOCKER_CMD"
  fi

  echo -e "Launching LeanEngine with arguments: "
  echo -e "$DOCKER_CMD"
  #Run built docker DOCKER_CMD;
  eval "$DOCKER_CMD"
else

  # First part of the docker DOCKER_CMD
  DOCKER_CMD="docker run -it --rm\
      --name $CONTAINER_NAME \
      -p 5678:5678 \
      --expose 6000 \
       $IMAGE"

  if [ "$MOUNT" != "Y" ]; then

    TEMP_DOCKERFILE="$(
      cat <<EOF
FROM quantconnect/lean:latest 
MAINTAINER QuantConnect <contact@quantconnect.com> 
RUN pip install ptvsd 
WORKDIR /Lean/ 
ENTRYPOINT ["/bin/bash"]
EOF
    )"

    EXCLUDE=${EXCLUDE:-$DEFAULT_EXCLUDE}
    FILES_DIRS=("$(find . -maxdepth 1 \! -name '.' \! -name '..')")
    # Parse exclusions
    if [[ "$EXCLUDE" != "None" ]]; then
      # Internal field separator -> array
      while IFS="," read -ra EXCLUDE; do
        for i in "${EXCLUDE[@]}"; do
          FILES_DIRS=("${FILES_DIRS[@]/$i/}")
        done
      done
    fi

    # Launch container, copy relevant folders
    for dir in "${FILES_DIRS[@]/$i/}"; do
      echo "COPY $dir /Lean/$dir" >> TEMP_DOCKERFILE
    done
    
  # Mount drive
  else
    TEMP_DOCKERFILE="$(
      cat <<EOF
FROM quantconnect/lean:latest 
MAINTAINER QuantConnect <contact@quantconnect.com> 
RUN pip install ptvsd 
WORKDIR /Lean/ 
ENTRYPOINT ["/bin/bash"]
EOF
    )"
    DOCKER_CMD+=" -v $(pwd):/$CONTAINER_NAME:rw"
  fi

  TEMP=$(mktemp ./.temp_dockerfile.XXXXXXXX)
  echo "$TEMP_DOCKERFILE" >> "$TEMP"
  DOCKERFILE="docker build -f $TEMP ."

  # Verify if user has docker permissions
  if ! touch /var/run/docker.sock &>/dev/null; then
    sudo -v
    SUDO="sudo"
    DOCKER_CMD="$SUDO $DOCKER_CMD"
    DOCKERFILE="$SUDO $DOCKERFILE"
  fi
  echo "Executing:"
  echo "###############DOCKERFILE###############"
  cat "$TEMP"
  echo "###############COMMAND###############"
  echo -e "$DOCKER_CMD"
  eval "$DOCKERFILE"
  eval "$DOCKER_CMD"
  rm TEMP
fi
