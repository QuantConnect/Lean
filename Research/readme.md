QuantConnect Research Project
=============

There is an up to date docker image available at [quantconnect/research](https://hub.docker.com/repository/docker/quantconnect/research). You can pull this image with `docker pull quantconnect/research`.


# Usage:
`docker run -it --rm -p 8888:8888 -v (absolute to your data folder):/home/Data:ro quantconnect/research`

# Build
`docker build -t quantconnect/research - < DockerfileJupyter` will build a new docker image using the latest version of lean. To build from particular tag of lean a build arg can be provided, for example `--build-arg LEAN_TAG=8631`.
