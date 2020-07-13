<h2>Local Development & Docker Integration w/ VS Code</h2>


This document contains information regarding ways to use Lean’s Docker image in conjunction with local development in Visual Studio Code.

<h2>Getting Setup</h2>


Before anything we need to ensure a few things have been done:



1. Get [Visual Studio Code](https://code.visualstudio.com/download)
    *   Get the Extension [Mono Debug](https://marketplace.visualstudio.com/items?itemName=ms-vscode.mono-debug)

2. Get [Docker](https://docs.docker.com/get-docker/):
    *   Follow the instructions for your Operating System
    *   New to Docker? Try docker getting-started

3. Pull Lean’s latest image from a terminal
    *   _docker pull quantconnect/lean_

4. Clone LEAN Locally
    *   git clone[ https://github.com/QuantConnect/Lean.git](https://github.com/QuantConnect/Lean.git)

5. Open the folder in VS Code

<h2>Develop Algorithms Locally, Run in Container</h2>


We have set up a relatively easy way to develop algorithms in your local IDE and push them into the container to be run and debugged.

Before we can use this method with Windows or Mac OS we need to share the Lean directory with Docker.

<h3>Activate File Sharing for Docker:</h3>




    *   Windows
        1. [https://docs.docker.com/docker-for-windows/#file-sharing](https://docs.docker.com/docker-for-windows/#file-sharing)
        2. Share the LEAN root directory with docker
    *   Mac
        3. [https://docs.docker.com/docker-for-mac/#file-sharing](https://docs.docker.com/docker-for-mac/#file-sharing)
        4. Share the LEAN root directory with docker

<h3>Lean Configuration</h3>


Next we need to be sure that our Lean configuration at .\Launcher\config.json is properly set. Just like running lean locally the config must reflect what we want Lean to run.

You configuration file should look something like this for the following languages:

<h4>Python:</h4>


"algorithm-type-name": "_~AlgorithmName~_",

"algorithm-language": "Python",

"algorithm-location": "../../../Algorithm.Python/_~AlgorithmName~_.py",

<h4>C#:</h4>


"algorithm-type-name": "_~AlgorithmName~_",

"algorithm-language": "CSharp",

"algorithm-location": "QuantConnect.Algorithm.CSharp.dll",

<h4>Note About C#</h4>


In order to use a custom C# algorithm, the C# file must be compiled before running in the docker, as it is compiled into the file "QuantConnect.Algorithm.CSharp.dll". Any new C# files will need to be added to the csproj compile list before it will compile, check Algorithm.CSharp/QuantConnect.Algorithm.CSharp.csproj for all algorithms that are compiled. Once there is an entry for your algorithm the project can be compiled by using the “build” task under _“Terminal” > “Run Build Task”._ 

Python **does not** have this requirement as the engine will compile it on the fly.

<h3>Running Lean in the Container</h3>


Now in VS Code click on the debug/run icon on the left toolbar, at the top you should see a drop down menu with launch options, be sure to select “Debug in Container”. This option will kick off a script that will walk you through all the required steps to start the Lean engine inside of the docker container. 

The steps the script takes are as follows:



*   Enter docker image [default: quantconnect/lean:latest]:
*   Enter absolute path to Lean config file [default: _~currentDir_\Launcher\config.json]:
*   Enter absolute path to Data folder [default: ~_currentDir_\Data\]:
*   Enter absolute path to store results [default: ~_currentDir_\]:
*   Are you using a custom algorithm? (Must be defined in config) [Y/N default: N]:

Using just the defaults (pressing enter) is fine except on the question of running a custom algorithm. If you enter “Y” you will be prompted with a few more questions depending on the language of the algorithm you are attempting to import to the container.

At this point VS Code will attach to the remote process in the container and run the debugger in the local IDE for everything C#. All logs will be passed to your store results directory (Default is root of Lean). Upon finishing the debug VS code will run a task to ensure that the docker container shuts down.

<h2>Debugging Python</h2>


Python algorithms require a little extra work in order to be able to debug them locally or in the container. Thankfully we were able to configure VS code tasks to take care of the work for you!

This particular setup requires 

<h3>Modifying the Configuration</h3>


First in order to debug a Python algorithm locally or in the container we must make the following change to our configuration (Launcher\config.json) under the comment debugging configuration:

 "debugging": true,

 "debugging-method": "PTVSD",

In setting this we are telling Lean to expect a debugger connection using ‘Python Tools for Visual Studio Debugger’. Once this is set Lean will stop upon initialization and await a connection to the debugger via port 5678.

<h3>Using VS Code Launch Options</h3>


Now that Lean is configured for the debugger we can make use of the programmed launch options to connect. 

<h4>Container</h4>


To debug inside of the container we must first start the container, follow the steps described in the section “[Running Lean in the Container](#Running-Lean-in-the-Container)”. Once the container is started you should see the messages in Figure 2.

If the message is displayed, use the same drop down for “Debug in Container” and select “Attach to Python (Container)”. Then press run, VS Code will now enter and debug any breakpoints you have set in your Python algorithm.

<h4>Local</h4>


To debug locally we must run the program locally using either the launch option “Launch Mono” (requires Mono but allows for C# debug at the same time) or Terminal > Run Task > “Run Application”. Once Lean is started you should see the messages in Figure 2.

If the message is displayed, use the launch option “Attach to Python (Local)”. Then press run, VS Code will now enter and debug any breakpoints you have set in your python algorithm.

<p>Figure 2: Python Debugger Messages


```
20200715 17:12:06.546 Trace:: PythonInitializer.Initialize(): ended
20200715 17:12:06.547 Trace:: DebuggerHelper.Initialize(): python initialization done
20200715 17:12:06.547 Trace:: DebuggerHelper.Initialize(): starting...
20200715 17:12:06.548 Trace:: DebuggerHelper.Initialize(): waiting for debugger to attach at localhost:5678...
```
