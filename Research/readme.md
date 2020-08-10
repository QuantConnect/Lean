QuantConnect Research Project
=============

There is an up to date docker image available at [quantconnect/research](https://hub.docker.com/repository/docker/quantconnect/research). You can pull this image with `docker pull quantconnect/research`.


# Using the Docker Image
The docker image we created can be started using the included .bat/.sh file in this directory (Lean/Research). These scripts take care of all the work required to get the notebook container setup and started for use. Including launching a browser to the notebook lab environment for you.

From a terminal launch the run_docker.bat/.sh script; there are a options on how to launch this:
 1. Launch with no parameters and answer the questions regarding configuration (Press enter for defaults) ex: `./run_docker_notebook.bat`
   
        *   Enter docker image [default: quantconnect/research:latest]:
        *   Enter absolute path to Data folder [default: ~yourpathtolean~\Lean\Data\]:
        *   Enter absolute path to store notebooks [default: ~yourpathtolean~\Lean\Research\Notebooks]:

 2. Using the **docker.cfg** to store args for repeated use; any blank entries will resort to default values! ex: `./run_docker_notebook.bat docker.cfg`
  
        image=
        data_dir=
        notebook_dir=

 3. Inline arguments; anything you don't enter will use the default args! ex: `./run_docker.bat image=quantconnect/research:latest`
      *    Accepted args for inline include all listed in the file **docker.cfg**



# Build a new image
For most users this will not be necessary, simply use `docker pull quantconnect/research` to get the latest image.


`docker build -t quantconnect/research - < DockerfileJupyter` will build a new docker image using the latest version of lean. To build from particular tag of lean a build arg can be provided, for example `--build-arg LEAN_TAG=8631`.
