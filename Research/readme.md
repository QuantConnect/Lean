QuantConnect Research Project
=============
Currently we have a few ways to use QuantConnect research notebooks:
- Lean CLI (Recommended)
- Install locally and run directly on your OS.

This document will cover the setup, getting started, and known issues.

<br>

# Setup
Below we cover how to get setup with our two options listed above. 

<br>

## Research with Lean CLI (Recommended)

Our research docker image has been integrated with Lean CLI to streamline the process and allow user to use their cloud and local projects in the research environment. Please refer to Lean CLI documentation [here](https://www.quantconnect.com/docs/v2/lean-cli/getting-started/lean-cli) on how to get started.

Lean CLI research specific documentation is found [here](https://www.quantconnect.com/docs/v2/lean-cli/tutorials/research).

We highly recommend using Lean CLI with docker for research but below in [Running Jupyter Locally](#running-jupyter-locally) we cover how to install and prepare the environment on your personal desktop. 

<br>

## Running Jupyter Locally 
Note: we recommend using the above approach with our Docker container, where the setup and environment is tested and stable.

Before we enable Jupyter support, follow [Lean installation](https://github.com/QuantConnect/Lean#installation-instructions)
and [Python installation](https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#quantconnect-python-algorithm-project) to get LEAN running Python algorithms on your machine. Then be sure to build Lean at least once before the following. 

**1. Installation:**
   1. Install [JupyterLab](https://pypi.org/project/jupyterlab/):
```
    pip install jupyterlab
```
 2.  Install [QuantConnect Python API](https://pypi.python.org/pypi/quantconnect/0.1)
 ```
    pip install quantconnect
```
 3.  Install [pythonnet/clr-loader](https://github.com/pythonnet/clr-loader)
 ```
    pip install clr-loader
```
**2. Run Jupyter:**
   1. Run Jupyter from the command line
```
    cd Lean/Launcher/bin/Debug
    jupyter lab
```
<br>

# Getting Started with Research

## C# Notebook
When using C# for research notebooks it requires that you load our setup script CSX file `Initialize.csx` into your notebook. This will load our QuantConnect libraries into your C# Kernel. In both docker setups, the file is one directory above the notebooks dir. Be sure to use the following line in your first cell to load in this csx file:

`#load "../Initialize.csx"`

After this the environment is ready to use; take a look at our reference notebook `KitchenSinkCSharpQuantBookTemplate.ipynb` for an example of how to use our `QuantBook` interface!

Note: All Lean namespaces you want to use in your notebook need to be directly added via `using` statements.

<br>

## Python Notebook
With Python we have a setup script that will automatically load QuantBooks libraries into the Python kernel so there is no need to import them. In our docker image the script should run automatically, but locally you will need to call `%run "start.py"` in the first cell.

You notebook is ready to use; take a look at our reference notebook `KitchenSinkQuantBookTemplate.ipynb` for an example of how to use our `QuantBook` interface!

<br>

## Using the Web Api from Notebook
Both of our setup scripts for Python & C# include a instantiated `Api` object under the variable name `api`. Before you can use this api object to interact with the cloud you must edit your config in the **root** of your Notebook directory. Once this has been done once, it does not need to be done again.

In `config.json` add the following entries with your respective values
```
job-user-id: 12345, // Your id here
api-access-token: "token13432", // Your api token here
```

Once this has been done, you may restart your kernel and begin to use the `api` variable. 
Reference our examples mentioned above for practical uses of this object.

<br>

## Shutting Down the Notebook Lab
When you are done with the research environment be sure to stop the container with **Docker Dashboard** or via **Docker CLI**.

<br>


## Build a new image
For most users this will not be necessary, simply use `docker pull quantconnect/research` to get the latest image.

`docker build -t quantconnect/research - < DockerfileJupyter` will build a new docker image using the latest version of lean. To build from particular tag of lean a build arg can be provided, for example `--build-arg LEAN_TAG=8631`.

<br>


# Known Issues
- Python research is extremely dependent on the `start.py` script as it is responsible for assigning core clr as the runtime for PythonNet and clr-loader to use for C# library. For local use where the script is not launched automatically by Jupyter, one must call `%run "start.py"` in their first notebook cell for research to work properly. Note that the location of `start.py` is in the launcher bin directory so you may have to use `../start.py` or specify the full path.

- C# research latest kernel no longer supports using statements outside of the notebook context, meaning that `#load ./QuantConnect.csx` no longer applies QC namespaces to the notebook out of the box. Therefore one must specify the namespace directly in a cell. Our default notebooks include these statements as examples.

- Python can sometimes have issues when paired with our quantconnect stubs package on Windows. This issue can cause modules not to be found because `site-packages` directory is not present in the python path. If you have the required modules installed and are seeing errors about them not being found, please try the following steps:
    - remove stubs -> pip uninstall quantconnect-stubs
    - reinstall stubs -> pip install quantconnect-stubs