<h1>Local Development & Docker Integration with Pycharm</h1>

This document contains information regarding ways to use Lean’s Docker image in conjunction with local development in Pycharm.


<br />

<h1>Getting Setup</h1>


Before anything we need to ensure a few things have been done:


1. Get [Pycharm Professional](https://www.jetbrains.com/pycharm/)**

2. Get [Docker](https://docs.docker.com/get-docker/):
    *   Follow the instructions for your Operating System
    *   New to Docker? Try docker getting-started


3. Pull Lean’s latest image from a terminal
    *   _docker pull quantconnect/lean_

4. Get Lean into Pycharm
    *   Download the repo or clone it using: _git clone[ https://github.com/QuantConnect/Lean](https://github.com/QuantConnect/Lean)_
    *   Open the folder using Pycharm


_**PyCharm’s remote debugger requires PyCharm Professional._

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

You configuration file should look something like this:

<h3>Python:</h3>

    "algorithm-type-name": "**AlgorithmName**",

    "algorithm-language": "Python",

    "algorithm-location": "../../../Algorithm.Python/**AlgorithmName**.py",

<h4>Note About Python Algorithm Location</h4>


Our specific configuration binds the Algorithm.Python directory to the container by default so any algorithm you would like to run should be in that directory. Please ensure your algorithm location looks just the same as the example above. If you want to use a different location refer to the section bellow on setting that argument for the container and make sure your config.json also reflects this.


<br />

<h2>Running Lean in the Container</h2>

This section will cover how to actually launch Lean in the container with your desired configuration.

From a terminal; Pycharm has a built in terminal on the bottom taskbar labeled **Terminal**; launch the run_docker.bat/.sh script; there are a few choices on how to launch this:
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
      *    Accepted args for inline include all listed in the file in #2; must follow the **key=value** format

<br />

<h1>Debugging Python</h1>

Debugging your Python algorithms requires an extra step within your configuration and inside of PyCharm. Thankfully we were able to configure the PyCharm launch configurations to take care of most of the work for you! 

<br />

<h2>Modifying the Configuration</h2>

First in order to debug a Python algorithm in Pycharm we must make the following change to our configuration (Launcher\config.json) under the comment debugging configuration:

    "debugging": true,
    "debugging-method": "PyCharm",


In setting this we are telling Lean to reach out and create a debugger connection using PyCharm’s PyDevd debugger server. Once this is set Lean will **always** attempt to connect to a debugger server on launch. **If you are no longer debugging set “debugging” to false.**

<br />

<h2>Using PyCharm Launch Options</h2>


Now that Lean is configured for the debugger we can make use of the programmed launch options to connect. 



**<h3>Container (Recommended)</h3>**


To debug inside of the container we must first start the debugger server in Pycharm, to do this use the drop down configuration “Debug in Container” and launch the debugger. Be sure to set some breakpoints in your algorithms!

Then we will need to launch the container, follow the steps described in the section “[Running Lean in the Container](#Running-Lean-in-the-Container)”. After launching the container the debugging configuration will take effect and it will connect to the debug server where you can begin debugging your algorithm.


**<h3>Local</h3>**


To debug locally we must run the program locally. First, just as the container setup, start the PyCharm debugger server by running the “Debug Local” configuration.

Then start the program locally by whatever means you typically use, such as Mono, directly running the program at **QuantConnect.Lean.Launcher.exe**, etc. Once the program is running it will make the connection to your PyCharm debugger server where you can begin debugging your algorithm.
