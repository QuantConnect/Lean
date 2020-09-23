<h1>Local Development & Docker Integration with Visual Studio</h1>


This document contains information regarding ways to use Visual Studio to work with the Lean's Docker image.


<br />

<h1>Getting Setup</h1>


Before anything we need to ensure a few things have been done:


1. Get [Visual Studio](https://code.visualstudio.com/download)
    *   Get the Extension [VSMonoDebugger](https://marketplace.visualstudio.com/items?itemName=GordianDotNet.VSMonoDebugger0d62) for C# Debugging

2. Get [Docker](https://docs.docker.com/get-docker/):
    *   Follow the instructions for your Operating System
    *   New to Docker? Try docker getting-started


3. Pull Lean’s latest image from a terminal
    *   _docker pull quantconnect/lean_

4. Get Lean into Visual Studio
    *   Download the repo or clone it using: _git clone[ https://github.com/QuantConnect/Lean](https://github.com/QuantConnect/Lean)_
    *   Open the solution **QuantConnect.Lean.sln** using Visual Studio


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

<br />

<h2>Important Note About C#</h2>

In order to use a custom C# algorithm, the C# file must be compiled before running in the docker, as it is compiled into the file **"QuantConnect.Algorithm.CSharp.dll"**. Any new C# files will need to be added to the csproj compile list before it will compile, check **Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj** for all algorithms that are compiled. Once there is an entry for your algorithm the project can be compiled by using  **Build > Build Solution**.

If you would like to debug this file in the docker container one small change to the solutions target build is required.
1. Right click on the solution **QuantConnect.Lean** in the _Solution Explorer_
2. Select **Properties**
3. For project entry **QuantConnect.Algorithm.CSharp** change the configuration to **DebugDocker**
4. Select **Apply** and close out of the window.
5. Build the project at least once before running the docker.

<br />

<h2>Running Lean in the Container</h2>

This section will cover how to actually launch Lean in the container with your desired configuration.

From a terminal launch the run_docker.bat/.sh script; there are a few choices on how to launch this:
 1. Launch with no parameters and answer the questions regarding configuration (Press enter for defaults)
   
        *   Enter docker image [default: quantconnect/lean:latest]:
        *   Enter absolute path to Lean config file [default: _~currentDir_\Launcher\config.json]:
        *   Enter absolute path to Data folder [default: ~_currentDir_\Data\]:
        *   Enter absolute path to store results [default: ~_currentDir_\]:
        *   Would you like to debug C#? (Requires mono debugger attachment) [default: N]:

 2. Using the **run_docker.cfg** to store args for repeated use; any blank entries will resort to default values! example: **_./run_docker.bat run_docker.cfg_**
  
        image=quantconnect/lean:latest
        config_file=
        data_dir=
        results_dir=
        debugging=
        python_dir=

 3. Inline arguments; anything you don't enter will use the default args! example: **_./run_docker.bat debugging=y_** 
      *    Accepted args for inline include all listed in the file in #2

<br />

<h1>Connecting to Mono Debugger</h1>

If you launch the script with debugging set to **yes** (y), then you will need to connect to the debugging server with the mono extension that you installed in the setup stage.

To setup the extension do the following:
   * Go to **Extensions > Mono > Settings...**
   * Enter the following for the settings:
     * Remote Host IP: 127.0.0.1
     * Remote Host Port: 55555
     * Mono Debug Port: 55555
   * Click **Save** and then close the extension settings
  
Now that the extension is setup use it to connect to the Docker container by using:
*  **Extensions > Mono > Attach to mono debugger**

The program should then launch and trigger any breakpoints you have set in your C# Algorithm.
