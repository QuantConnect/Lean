#!/bin/bash

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

full_path=$(realpath $0)
current_dir=$(dirname $full_path)
default_image=quantconnect/research:latest
parent=$(dirname $current_dir)
default_data_dir=$parent/Data
default_notebook_dir=$current_dir/Notebooks/

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
    read -p "Enter docker image [default: $default_image]: " image
    read -p "Enter absolute path to Data folder [default: $default_data_dir]: " data_dir
    read -p "Enter absolute path to store notebooks [default: $default_notebook_dir]: " notebook_dir
fi

if [ -z "$image" ]; then
    image=$default_image
fi

if [ -z "$data_dir" ]; then
    data_dir=$default_data_dir
fi

if [ ! -d "$data_dir" ]; then
    echo "Data directory $data_dir does not exist"
    exit 1
fi

if [ -z "$notebook_dir" ]; then
    notebook_dir=$default_notebook_dir
fi

if [ ! -d "$notebook_dir" ]; then
    mkdir $notebook_dir
fi

echo "Starting docker container; container id is:"
sudo docker run -d --rm -p 8888:8888 \
    --mount type=bind,source=$data_dir,target=/Data,readonly \
    --mount type=bind,source=$notebook_dir,target=/Lean/Launcher/bin/Debug/Notebooks \
    $image

echo "Docker container started; will wait 2 seconds before opening web browser."
sleep 2s
xdg-open http://localhost:8888/lab

