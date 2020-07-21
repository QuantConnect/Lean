<h2>Local Development & Docker Integration w/ Pycharm</h2>


This document contains information regarding ways to use Lean’s Docker image in conjunction with local development in Pycharm.

<h2>Getting Setup</h2>


Before anything we need to ensure a few things have been done:



1. Get [Pycharm Professional](https://www.jetbrains.com/pycharm/)**

2. Get [Docker](https://docs.docker.com/get-docker/):
    *   Follow the instructions for your Operating System
    *   New to Docker? Try docker getting-started
3. Pull Lean’s latest image from a terminal
    *   _docker pull quantconnect/lean_

     
4. Clone LEAN Locally
    *   git clone[ https://github.com/QuantConnect/Lean.git](https://github.com/QuantConnect/Lean.git)

5. Open the folder in Pycharm

**PyCharm’s remote debugger requires PyCharm Professional.

<h2>Develop Algorithms Locally, Run in Container</h2>


We have set up a relatively easy way to develop algorithms in your local IDE and push them into the container to be run.

Before we can use this method with Windows or Mac OS we need to share the Lean directory with Docker.

<h3>Activate File Sharing for Docker:</h3>




    *   Windows
        1. [https://docs.docker.com/docker-for-windows/#file-sharing](https://docs.docker.com/docker-for-windows/#file-sharing)
        2. Share the LEAN root directory with docker
    *   Mac
        3. [https://docs.docker.com/docker-for-mac/#file-sharing](https://docs.docker.com/docker-for-mac/#file-sharing)
        4. Share the LEAN root directory with docker


<h3>Lean Configuration</h3>


Next we need to be sure that our Lean configuration at .\Launcher\config.json is properly set. Just like running lean locally, the config must reflect what we want Lean to run. In the case of Pycharm we will only be using python algorithms so follow the steps below to ensure your algorithms will run in the docker container. 

You configuration file should look something like this:

<h4>Python:</h4>


"algorithm-type-name": "_~AlgorithmName~_",

"algorithm-language": "Python",

"algorithm-location": "../../../Algorithm.Python/_~AlgorithmName~_.py",

<h4>Note About Python Algorithm Location</h4>


Our specific configuration binds the Algorithm.Python directory to the container so any algorithm you would like to run must be in that directory. Please ensure your algorithm location looks just the same as the example above.

<h3>Running Lean in the Container</h3>


Now that your configuration is all set and your algorithm in the correct location use the run_docker.bat/.sh script located in Lean’s root directory. The script will kick off the process of starting the container.

The steps the script takes are as follows:



*   Enter docker image [default: quantconnect/lean:latest]:
*   Enter absolute path to Lean config file [default: _~currentDir_\Launcher\config.json]:
*   Enter absolute path to Data folder [default: ~_currentDir_\Data\]:
*   Enter absolute path to store results [default: ~_currentDir_\]:

Using just the defaults (pressing enter) is fine. For most users there is no need for customization. At this point the container will launch and run Lean with your configuration. All logs will be passed to your store results directory (Default is root of Lean). If you would like to debug your algorithm follow the steps in the next section to modify your configuration.

<h2>Debugging Python</h2>


Debugging your Python algorithms requires an extra step within your configuration and inside of PyCharm. Thankfully we were able to configure the PyCharm launch configurations to take care of most of the work for you! 

<h3>Modifying the Configuration</h3>


First in order to debug a Python algorithm locally or in the container we must make the following change to our configuration (Launcher\config.json) under the comment debugging configuration:


 "debugging": true,

 "debugging-method": "PyCharm",


In setting this we are telling Lean to reach out and create a debugger connection using PyCharm’s PyDevd debugger server. Once this is set Lean will **always** attempt to connect to a debugger server on launch.** **If you are no longer debugging set “debugging” to false.

<h3>Using PyCharm Launch Options</h3>


Now that Lean is configured for the debugger we can make use of the programmed launch options to connect. 

<h4>Container</h4>


To debug inside of the container we must first start the debugger server in Pycharm, to do this use the drop down configuration “Debug in Container” and launch the debugger. Be sure to set some breakpoints in your algorithms!

Then we will need to launch the container, follow the steps described in the section “[Running Lean in the Container](#Running Lean in the Container)”. After launching the container the debugging configuration will take effect and it will connect to the debug server where you can begin debugging your algorithm.

<h4>Local</h4>


To debug locally we must run the program locally. First, just as the container setup, start the PyCharm debugger server by running the “Debug Local” configuration.

Then start the program locally by whatever means you typically use, such as Mono, directly running the program at QuantConnect.Lean.Launcher.exe, etc. Once the program is running it will make the connection to your PyCharm debugger server where you can begin debugging your algorithm.
