<h1>Local Development & Docker Integration with Visual Studio Code</h1>


This document contains information regarding ways to use Visual Studio Code to work with the Lean engine, this includes using Lean’s Docker image in conjunction with local development as well as running Lean locally.


<br />

<h1>Getting Setup</h1>


Before anything we need to ensure a few things have been done:


1. Get [Visual Studio Code](https://code.visualstudio.com/download)
    *   Get the Extension [Mono Debug **15.8**](https://marketplace.visualstudio.com/items?itemName=ms-vscode.mono-debug) for C# Debugging
    *   Get the Extension [Python](https://marketplace.visualstudio.com/items?itemName=ms-python.python) for Python Debugging

2. Get [Docker](https://docs.docker.com/get-docker/):
    *   Follow the instructions for your Operating System
    *   New to Docker? Try docker getting-started

3. Install a compiler for the project **(Only needed for C# Debugging or Running Locally)**
   *   On Linux or Mac: 
       *   Install [mono-complete](https://www.mono-project.com/docs/getting-started/install/linux/)
       *   Test msbuild with command: _msbuild -version_
   *   On Windows: 
       *   Visual Studio comes packed with msbuild or download without VS [here](https://visualstudio.microsoft.com/downloads/?q=build+tools)
       *   Put msbuild on your system path and test with command: _msbuild -version_

4. Pull Lean’s latest image from a terminal
    *   _docker pull quantconnect/lean_

5. Get Lean into VS Code
    *   Download the repo or clone it using: _git clone[ https://github.com/QuantConnect/Lean](https://github.com/QuantConnect/Lean)_
    *   Open the folder using VS Code

**NOTES**: 
- Mono Extension Version 16 and greater fails to debug the docker container remotely, please install **Version 15.8**. To install an older version from within VS Code go to the extensions tab, search "Mono Debug", and select "Install Another Version...".
<br />

<h1>Develop Algorithms Locally, Run in Container</h1>


We have set up a relatively easy way to develop algorithms in your local IDE and push them into the container to be run and debugged. 

Before we can use this method with Windows or Mac OS we need to share the Lean directory with Docker.

<br />

<h2>Activate File Sharing for Docker:</h2>

*   Windows: 
    *   [Guide to sharing](https://docs.docker.com/docker-for-windows/#file-sharing)
    *   Share the LEAN root directory with docker
  
*   Mac:
    *   [Guide to sharing](https://docs.docker.com/docker-for-mac/#file-sharing)
    *   Share the LEAN root directory with docker

*   Linux:
    *    (No setup required)

<br />

<h2>Lean Configuration</h2>

Next we need to be sure that our Lean configuration at **.\Launcher\config.json** is properly set. Just like running lean locally the config must reflect what we want Lean to run.

You configuration file should look something like this for the following languages:

<h3>Python:</h3>

    "algorithm-type-name": "**AlgorithmName**",

    "algorithm-language": "Python",

    "algorithm-location": "../../../Algorithm.Python/**AlgorithmName**.py",

<h3>C#:</h3>

    "algorithm-type-name": "**AlgorithmName**",

    "algorithm-language": "CSharp",

    "algorithm-location": "QuantConnect.Algorithm.CSharp.dll",


<h3>Important Note About C#</h3>

In order to use a custom C# algorithm, the C# file must be compiled before running in the docker, as it is compiled into the file "QuantConnect.Algorithm.CSharp.dll". Any new C# files will need to be added to the csproj compile list before it will compile, check Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj for all algorithms that are compiled. Once there is an entry for your algorithm the project can be compiled by using the “build” task under _“Terminal” > “Run Build Task”._ 

Python **does not** have this requirement as the engine will compile it on the fly.

<br />

<h2>Running Lean in the Container</h2>

This section will cover how to actually launch Lean in the container with your desired configuration.

<br />

<h3>Option 1 (Recommended)</h3>

In VS Code click on the debug/run icon on the left toolbar, at the top you should see a drop down menu with launch options, be sure to select **Debug in Container**. This option will kick off a launch script that will start the docker. With this specific launch option the parameters are already configured in VS Codes **tasks.json** under the **run-docker** task args. These set arguments are:

    "IMAGE=quantconnect/lean:latest",
    "CONFIG_FILE=${workspaceFolder}/Launcher/config.json",
    "DATA_DIR=${workspaceFolder}/Data",
    "RESULTS_DIR=${workspaceFolder}/Results",
    "DEBUGGING=Y",
    "PYHTON_DIR=${workspaceFolder}/Algorithm.Python"

As defaults these are all great! Feel free to change them as needed for your setup.

**NOTE:** VSCode may try and throw errors when launching this way regarding build on `QuantConnect.csx` and `Config.json` these errors can be ignored by selecting "*Debug Anyway*". To stop this error message in the future select "*Remember my choice in user settings*". 

If using C# algorithms ensure that msbuild can build them successfully.  



<br />

<h3>Option 2</h3>

From a terminal launch the run_docker.bat/.sh script; there are a few choices on how to launch this:
 1. Launch with no parameters and answer the questions regarding configuration (Press enter for defaults)
   
        *   Enter docker image [default: quantconnect/lean:latest]:
        *   Enter absolute path to Lean config file [default: .\Launcher\config.json]:
        *   Enter absolute path to Data folder [default: .\Data\]:
        *   Enter absolute path to store results [default: .\Results]:
        *   Would you like to debug C#? (Requires mono debugger attachment) [default: N]:

 2. Using the **run_docker.cfg** to store args for repeated use; any blank entries will resort to default values! example: **_./run_docker.bat run_docker.cfg_**
  
        IMAGE=quantconnect/lean:latest
        CONFIG_FILE=
        DATA_DIR=
        RESULTS_DIR=
        DEBUGGING=
        PYTHON_DIR=

 3. Inline arguments; anything you don't enter will use the default args! example: **_./run_docker.bat DEBUGGING=y_** 
      *    Accepted args for inline include all listed in the file in #2

<br />

<h1>Debugging Python</h1>

Python algorithms require a little extra work in order to be able to debug them locally or in the container. Thankfully we were able to configure VS code tasks to take care of the work for you! Follow the steps below to get Python debugging working.

<br />

<h2>Modifying the Configuration</h2>

First in order to debug a Python algorithm in VS Code we must make the following change to our configuration (Launcher\config.json) under the comment debugging configuration:

    "debugging": true,
    "debugging-method": "PTVSD",

In setting this we are telling Lean to expect a debugger connection using ‘Python Tools for Visual Studio Debugger’. Once this is set Lean will stop upon initialization and await a connection to the debugger via port 5678.

<br />

<h2>Using VS Code Launch Options to Connect</h2>

Now that Lean is configured for the python debugger we can make use of the programmed launch options to connect. 

<br />

<h3>Container</h3>


To debug inside of the container we must first start the container, follow the steps described in the section “[Running Lean in the Container](#Running-Lean-in-the-Container)”. Once the container is started you should see the messages in Figure 2.

If the message is displayed, use the same drop down for “Debug in Container” and select “Attach to Python (Container)”. Then press run, VS Code will now enter and debug any breakpoints you have set in your Python algorithm.

<br />

<h3>Local</h3>


To debug locally we must run the program locally using the programmed task found under Terminal > Run Task > “Run Application”. Once Lean is started you should see the messages in Figure 2.

If the message is displayed, use the launch option “Attach to Python (Local)”. Then press run, VS Code will now enter and debug any breakpoints you have set in your python algorithm.

<br />

_Figure 2: Python Debugger Messages_

```
20200715 17:12:06.546 Trace:: PythonInitializer.Initialize(): ended
20200715 17:12:06.547 Trace:: DebuggerHelper.Initialize(): python initialization done
20200715 17:12:06.547 Trace:: DebuggerHelper.Initialize(): starting...
20200715 17:12:06.548 Trace:: DebuggerHelper.Initialize(): waiting for debugger to attach at localhost:5678...
```

<br />

<h1>Common Issues</h1>
Here we will cover some common issues with setting this up. This section will expand as we get user feedback!

*   Any error messages about building in VSCode that point to comments in JSON. Either select **ignore** or follow steps described [here](https://stackoverflow.com/questions/47834825/in-vs-code-disable-error-comments-are-not-permitted-in-json) to remove the errors entirely.
*   `Errors exist after running preLaunchTask 'run-docker'`This VSCode error appears to warn you of CSharp errors when trying to use `Debug in Container` select "Debug Anyway" as the errors are false flags for JSON comments as well as `QuantConnect.csx` not finding references. Neither of these will impact your debugging. 
*   `The container name "/LeanEngine" is already in use by container "****"` This Docker error implies that another instance of lean is already running under the container name /LeanEngine. If this error appears either use Docker Desktop to delete the container or use `docker kill LeanEngine` from the command line.