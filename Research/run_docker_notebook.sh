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

DEFAULT_IMAGE=quantconnect/research:latest
DEFAULT_DATA_DIR=../Data
DEFAULT_NOTEBOOK_DIR=./Notebooks/
CONTAINER_NAME=LeanResearch

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

# realpath polyfill, notably absent macOS and some debian distros
absolute_path() {
  echo "$(cd "$(dirname "${1}")" && pwd)/$(basename "${1}")"
}

#If arg is a file process the key values
if [ -f "$1" ]; then
    IFS="="
    while read -r key value; do
        eval "$key='$value'"
    done < $1
#If there are in line args, process them
elif [ ! -z "$*" ]; then
    for arg in "$@"; do
        eval "$arg"
    done
#Else query user for settings
else
    read -p "Enter docker image [default: $DEFAULT_IMAGE]: " IMAGE
    read -p "Enter absolute path to Data folder [default: $DEFAULT_DATA_DIR]: " DATA_DIR
    read -p "Enter absolute path to store notebooks [default: $DEFAULT_NOTEBOOK_DIR]: " NOTEBOOK_DIR
fi

#Have to reset IFS for cfg files to work properly
IFS=" "

# fall back to defaults on empty input without
DATA_DIR=${DATA_DIR:-$DEFAULT_DATA_DIR}
NOTEBOOK_DIR=${NOTEBOOK_DIR:-$DEFAULT_NOTEBOOK_DIR}
IMAGE=${IMAGE:-$DEFAULT_IMAGE}

# convert to absolute paths
DATA_DIR=$(absolute_path "${DATA_DIR}")
NOTEBOOK_DIR=$(absolute_path "${NOTEBOOK_DIR}")

if [ ! -d "$DATA_DIR" ]; then
    echo "Data directory $DATA_DIR does not exist"
    exit 1
fi

if [ ! -d "$NOTEBOOK_DIR" ]; then
    mkdir $NOTEBOOK_DIR
fi

SUDO=""

# verify if user has docker permissions
if ! touch /var/run/docker.sock &>/dev/null; then
  sudo -v
  SUDO="sudo"
  COMMAND="$SUDO $COMMAND"
fi

#Check if the container is running already
if [ "$($SUDO docker container inspect -f '{{.State.Running}}' $CONTAINER_NAME)" == "true" ]; then
  yes_or_no "A Lean container is already running. Stop and recreate with this configuration?" &&
    ($SUDO docker stop $CONTAINER_NAME)
elif $SUDO docker ps -a | grep -q $CONTAINER_NAME; then
  yes_or_no "A Lean container is halted and will be removed. Continue?" &&
    $SUDO docker rm $CONTAINER_NAME
fi

echo "Starting docker container; container id is:"
sudo docker run -d --rm -p 8888:8888 \
    --mount type=bind,source=$DATA_DIR,target=/home/Data,readonly \
    --mount type=bind,source=$NOTEBOOK_DIR,target=/Lean/Launcher/bin/Debug/Notebooks \
    --name $CONTAINER_NAME \
    $IMAGE

echo "Docker container started; will wait 2 seconds before opening web browser."
sleep 2s
xdg-open http://localhost:8888/lab

