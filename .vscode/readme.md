<h1>Local Development & Docker Integration with Visual Studio Code</h1>

This document contains information regarding ways to use Visual Studio Code to work with the Lean engine, this includes a couple options that make lean easy to develop on any machine:

- Using Lean CLI -> A great tool for working with your algorithms locally, while still being able to deploy to the cloud and have access to Lean data. It is also able to run algorithms locally through our official docker images **Recommended for algorithm development.

- Using a Lean Dev container -> A docker environment with all dependencies pre-installed to allow seamless Lean development across platforms. Great for open source contributors.

- Locally installing all dependencies to run Lean with Visual Studio Code on your OS.

<br />

<h1>Setup</h1>

<h2>Option 1: Lean CLI</h2>

To use Lean CLI follow the instructions for installation and tutorial for usage in our [documentation](https://www.quantconnect.com/docs/v2/lean-cli/key-concepts/getting-started)

<br />

<h2>Option 2: Lean Development Container</h2>

Before anything we need to ensure a few things have been done for either option:

1. Get [Visual Studio Code](https://code.visualstudio.com/download)
    - Get [Remote Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) Extension

2. Get [Docker](https://docs.docker.com/get-docker/):
    - Follow the instructions for your Operating System
    - New to Docker? Try [docker getting-started](https://docs.docker.com/get-started/)

3. Pull Lean’s latest research image from a terminal
    - `docker pull quantconnect/research:latest`

4. Get Lean into VS Code
    - Download the repo or clone it using: `git clone [https://github.com/QuantConnect/Lean](https://github.com/QuantConnect/Lean)`
    - Open the folder using VS Code

5. Open Development Container
    - In VS Code, either:
        - Select "Reopen in Container" from pop up box.

            OR

        - Ctrl+Shift+P (Command Palette) and select "Remote-Containers: Rebuild and Reopen in Container"

You should now be in the development container, give VS Code a moment to prepare and you will be ready to go!
If you would like to mount any additional local files to your container, checkout [devcontainer.json "mounts" section](https://containers.dev/implementors/json_reference/) for an example! Upon any mount changes you must rebuild the container using Command Palette as in step 5.

<br />

<h2>Option 3: Install Dependencies Locally</h2>

1. Install [.NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) for the project

2. (Optional) Get [Python 3.8.13](https://www.python.org/downloads/release/python-3813/) for running Python algorithms
    - Follow Python instructions [here](https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#installing-python-38) for your platform

3. Get [Visual Studio Code](https://code.visualstudio.com/download)
    - Get the Extension [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) for C# Debugging
    - Get the Extension [Python](https://marketplace.visualstudio.com/items?itemName=ms-python.python) for Python Debugging

4. Get Lean into VS Code
    - Download the repo or clone it using: `git clone [https://github.com/QuantConnect/Lean](https://github.com/QuantConnect/Lean)`
    - Open the folder using VS Code

Your environment is prepared and ready to run Lean.

<br />

<h1>How to use Lean</h1>

This section will cover configuring, building, launching and debugging lean. This is only applicable to option 2 from above. This does not apply to Lean CLI, please refer to [CLI documentation](https://www.quantconnect.com/docs/v2/lean-cli/key-concepts/getting-started)

<br />

<h2>Configuration</h2>

We need to be sure that our Lean configuration at **.\Launcher\config.json** is properly set.

Your configuration file should look something like this for the following languages:

<h3>Python:</h3>

    "algorithm-type-name": "**AlgorithmName**",

    "algorithm-language": "Python",

    "algorithm-location": "../../../Algorithm.Python/**AlgorithmName**.py",

<h3>C#:</h3>

    "algorithm-type-name": "**AlgorithmName**",

    "algorithm-language": "CSharp",

    "algorithm-location": "QuantConnect.Algorithm.CSharp.dll",

<br />

<h2>Building</h2>

Before running Lean, we must build the project. Currently the VS Code task will automatically build before launching. But find more information below about how to trigger building manually.

In VS Code run build task (Ctrl+Shift+B or "Terminal" dropdown); there are a few options:

- __Build__ - basic build task, just builds Lean once
- __Rebuild__ - rebuild task, completely rebuilds the project. Use if having issues with debugging symbols being loaded for your algorithms.
- __Clean__ - deletes out all project build files

<br />

<h2>Launching Lean</h2>

Now that lean is configured and built we can launch Lean. Under "Run & Debug" use the launch option "Launch". This will start Lean with C# debugging. Any breakpoints in Lean C# will be triggered.

<br />

<h2>Debugging Python</h2>

Python algorithms require a little extra work in order to be able to debug them. Follow the steps below to get Python debugging working.

<br />

<h3>Modifying the Configuration</h3>

First in order to debug a Python algorithm in VS Code we must make the following change to our configuration (Launcher\config.json) under the comment debugging configuration:

    "debugging": true,
    "debugging-method": "DebugPy",

In setting this we are telling Lean to expect a debugger connection using ‘Python Tools for Visual Studio Debugger’. Once this is set Lean will stop upon initialization and await a connection to the debugger via port 5678.

<br />

<h3>Using VS Code Launch Options to Connect</h3>

Now that Lean is configured for the python debugger we can make use of the programmed launch options to connect to Lean during runtime.

Start Lean using the "Launch" option covered above. Once Lean starts you should see the messages in figure 2 If the message is displayed, use the launch option “Attach to Python”. Then press run, VS Code will now enter and debug any breakpoints you have set in your python algorithm.

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

- The "project file cannot be loaded" and "nuget packages not found" errors occurs when the project files are open by another process in the host. Closing all applications and/or restarting the computer solve the issue.
- Autocomplete and reference finding with omnisharp can sometimes be buggy, if this occurs use the command palette to restart omnisharp. (Ctrl+Shift+P "OmniSharp: Restart OmniSharp")
- Any error messages about building in VSCode that point to comments in JSON. Either select **ignore** or follow steps described [here](https://stackoverflow.com/questions/47834825/in-vs-code-disable-error-comments-are-not-permitted-in-json) to remove the errors entirely.
