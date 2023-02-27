<h1>Local Development with Visual Studio</h1>

This document contains information regarding ways to use Visual Studio to work with the Lean engine, this includes a couple options that make lean easy to develop on any machine:

- Using Lean CLI -> A great tool for working with your algorithms locally, while still being able to deploy to the cloud and have access to Lean data. It is also able to run algorithms locally through our official docker images **Recommended for algorithm development.

- Locally installing all dependencies to run Lean with Visual Studio on your OS.

<br />

<h1>Setup</h1>

<h2>Option 1: Lean CLI</h2>

To use Lean CLI follow the instructions for installation and tutorial for usage in our [documentation](https://www.quantconnect.com/docs/v2/lean-cli/getting-started/lean-cli).

<br />

<h2>Option 2: Install Locally</h2>

1. Install [.Net 6](https://dotnet.microsoft.com/download) for the project

2. (Optional) Get [Python 3.8.13](https://www.python.org/downloads/release/python-3813/) for running Python algorithms
    - Follow Python instructions [here](https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#installing-python-38) for your platform

3. Get [Visual Studio](https://visualstudio.microsoft.com/vs/)

4. Get Lean into VS
    - Download the repo or clone it using: _git clone [https://github.com/QuantConnect/Lean](https://github.com/QuantConnect/Lean)_
    - Open the project file with VS (QuantConnect.Lean.sln)

Your environment is prepared and ready to run lean

<br />

<h1>How to use Lean</h1>

This section will cover configuring, launching and debugging lean. This is only applicable to option 2 from above. This does not apply to Lean CLI, please refer to [CLI documentation](https://www.quantconnect.com/docs/v2/lean-cli/getting-started/lean-cli)

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

<h2>Launching Lean</h2>

Now that lean is configured we can launch. Use Visual Studio's run option, Make sure QuantConnect.Lean.Launcher is selected as the launch project. Any breakpoints in Lean C# will be triggered.

<br />

<h1>Common Issues</h1>
Here we will cover some common issues with setting this up. Feel free to contribute to this section!
