<h1>Local Development & Docker Integration with Visual Studio Code</h1>

This document contains information regarding ways to use Visual Studio Code to work with the Lean engine, this includes a couple options that make lean easy to develop on any machine:

- Using Lean CLI -> A great tool for working with your algorithms locally, while still being able to deploy to the cloud and have access to Lean data. It is also able to run algorithms locally through our official docker images **Recommended for algorithm development.

- Locally installing all dependencies to run Lean with Visual Studio Code on your OS.

<br />

<h1>Setup</h1>

<h2>Option 1: Lean CLI</h2>

To use Lean CLI follow the instructions for installation and tutorial for usage in our [documentation](https://www.quantconnect.com/docs/v2/lean-cli/getting-started/lean-cli)

<br />

<h2>Option 2: Install Dependencies Locally</h2>

1. Install [.Net 5](https://dotnet.microsoft.com/download) for the project

2. (Optional) Get [Python 3.6.8](https://www.python.org/downloads/release/python-368/) for running Python algorithms
    - Follow Python instructions [here](https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#installing-python-36) for your platform

3. Get [Visual Studio Code](https://code.visualstudio.com/download)
    - Get the Extension [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) for C# Debugging
    - Get the Extension [Python](https://marketplace.visualstudio.com/items?itemName=ms-python.python) for Python Debugging

4. Get Lean into VS Code
    - Download the repo or clone it using: _git clone [https://github.com/QuantConnect/Lean](https://github.com/QuantConnect/Lean)_
    - Open the folder using VS Code

Your environment is prepared and ready to run lean

<br />

<h1>How to use Lean</h1>

This section will cover configuring, building, launching and debugging lean. This is only applicable to option 2 from above. This does not apply to Lean CLI, please refer to [CLI documentation](https://www.quantconnect.com/docs/v2/lean-cli/getting-started/lean-cli)

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
- __Autobuilder__ - Starts a script that builds then waits for files to change and rebuilds appropriately
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
    "debugging-method": "PTVSD",

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

- Autocomplete and reference finding with omnisharp can sometimes bug, if this occurs use the command palette to restart omnisharp. (Ctrl+Shift+P "OmniSharp: Restart OmniSharp")
- Any error messages about building in VSCode that point to comments in JSON. Either select **ignore** or follow steps described [here](https://stackoverflow.com/questions/47834825/in-vs-code-disable-error-comments-are-not-permitted-in-json) to remove the errors entirely.
- Python Algorithms will only attach and debug correctly when the algorithm lives in ./Algorithm.Python, this is due to an issue where Lean runs the py files from the build dir at
runtime; so we have mapped the ./Algorithm.Python directory to the build dir for the debugger. The mapped directory can be changed in .vscode/launch.json under "Attach To Python"
option "LocalRoot". If you adjust this to your Py algorithm directory you will be able to launch and debug in the same way.
