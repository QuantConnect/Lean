QuantConnect Research Project
=============
Currently we have two ways to use QuantConnect research notebooks, you can either install and run locally or just use our docker image (Recommended).

The up to date docker image is available at [quantconnect/research](https://hub.docker.com/repository/docker/quantconnect/research). You can pull this image with `docker pull quantconnect/research`.




# Using the Docker Image
The docker image we created can be started using the included .bat/.sh file in this directory (Lean/Research). These scripts take care of all the work required to get the notebook container setup and started for use. Including launching a browser to the notebook lab environment for you.

From a terminal launch the run_docker_notebook.bat/.sh script; there are a few options on how to launch this:
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

Once the docker image starts, the script will attempt to open your browser to the Jupyter notebook web app, if this fails go to `localhost:8888`

When you are done with the research environment be sure to stop the container with either Docker's dashboard or through the CLI.

## Note for C#
When using C# for research notebooks it requires that you load our CSX file `QuantConnect.csx` into your notebook. In this setup, the file is one directory above the notebooks default dir. Be sure to use the following line to load in this csx file:

`load "../QuantConnect.csx"`

## Build a new image
For most users this will not be necessary, simply use `docker pull quantconnect/research` to get the latest image.


`docker build -t quantconnect/research - < DockerfileJupyter` will build a new docker image using the latest version of lean. To build from particular tag of lean a build arg can be provided, for example `--build-arg LEAN_TAG=8631`.


# Running Jupyter Locally

Before we enable Jupyter support, follow [Lean installation](https://github.com/QuantConnect/Lean#installation-instructions)
and [Python installation](https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#quantconnect-python-algorithm-project) to get LEAN running Python algorithms in your machine. 

**1. Installation:**
   1. Install [JupyterLab](https://pypi.org/project/jupyterlab/):
```
    pip install jupyterlab
```
 2.  Install [QuantConnect Python API](https://pypi.python.org/pypi/quantconnect/0.1)
 ```
    pip install quantconnect
```
 3. **Linux and macOS:** Copy pythonnet binaries for jupyter
 ```
  cp Lean/Launcher/bin/Debug/jupyter/* Lean/Launcher/bin/Debug
 ```
**2. Run Jupyter:**
   1. Update the `config.json` file in `Lean/Launcher/bin/Debug/` folder
 ```
    "composer-dll-directory": ".",
 ```
   2. Run Jupyter from the command line
```
    cd Lean/Launcher/bin/Debug
    jupyter lab
```