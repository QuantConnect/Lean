#!/bin/bash

docker build -t quantconnect/lean:foundation-$1 -t quantconnect/lean:foundation-$2 -f DockerfileLeanFoundation .
docker push quantconnect/lean:foundation-$1
docker push quantconnect/lean:foundation-$2

docker build -t quantconnect/lean:$1 -t quantconnect/lean:$2 .
docker push quantconnect/lean:$1
docker push quantconnect/lean:$2

docker build -t quantconnect/research:$1 -t quantconnect/research:$2 -f DockerfileJupyter .
docker push quantconnect/research:$1
docker push quantconnect/research:$2