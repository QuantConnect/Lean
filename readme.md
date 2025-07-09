<picture >
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/f3581da5-1983-4f6c-af5a-55c79b37913a">
  <source media="(prefers-color-scheme: light)" srcset="https://github.com/user-attachments/assets/0f8022d5-952d-418c-9011-2644830137d2">
  <img alt="lean-header" width="100%">
</picture>
<br />
<br />

[![Build Status](https://github.com/QuantConnect/Lean/workflows/Build%20%26%20Test%20Lean/badge.svg)](https://github.com/QuantConnect/Lean/actions?query=workflow%3A%22Build%20%26%20Test%20Lean%22) &nbsp;&nbsp;&nbsp; [![Regression Tests](https://github.com/QuantConnect/Lean/workflows/Regression%20Tests/badge.svg)](https://github.com/QuantConnect/Lean/actions?query=workflow%3A%22Regression%20Tests%22) &nbsp;&nbsp;&nbsp; [![LEAN Forum](https://img.shields.io/badge/debug-LEAN%20Forum-53c82b.svg)](https://www.quantconnect.com/forum/discussions/1/lean) &nbsp;&nbsp;&nbsp; [![Discord Chat](https://img.shields.io/badge/chat-Discord-53c82b.svg)](https://www.quantconnect.com/discord)


[Lean Home][1] | [Documentation][2] | [Download Zip][3] | [Docker Hub][8] | [Nuget][9]

#

<picture >
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/09d7707d-619d-48e2-b6d9-ef2d2d61144b">
  <source media="(prefers-color-scheme: light)" srcset="https://github.com/user-attachments/assets/aab2422c-f480-421d-9ad2-5a355843d82a">
  <img alt="features-header" width="100%">
</picture>

LEAN is an event-driven, professional-caliber algorithmic trading  platform built with a passion for elegant engineering and deep quant  concept modeling. Out-of-the-box alternative data and live-trading support.
<br/>
<br/>

<picture >
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/d0ca17eb-307f-4155-b989-9afe502845b9">
  <source media="(prefers-color-scheme: light)" srcset="https://github.com/user-attachments/assets/9135fa86-c3e3-48e6-bbf9-de97f17afb52">
  <img alt="feature-list" width="100%">
</picture>

<br/>
<br/>

#

<picture >
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/f486e040-e350-4c9b-98c5-7b3902c0b7d8">
  <source media="(prefers-color-scheme: light)" srcset="https://github.com/user-attachments/assets/d28fd3d4-dad8-4828-94a9-676ddb360bdd">
  <img alt="modular-header" width="100%">
</picture>
LEAN is modular in design, with each component pluggable and customizable. It ships with models for all major plug-in points.
<br/>
<br/>

<picture >
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/7989d185-45cd-4a40-acef-6ff61d9d82f6">
  <source media="(prefers-color-scheme: light)" srcset="https://github.com/user-attachments/assets/5f9cc976-a715-495a-9977-87961509d2e0">
  <img alt="modular-architecture" width="100%">
</picture>

#

<picture >
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/9b7b7abf-b0f5-41a3-8a1b-a9400738b27a">
  <source media="(prefers-color-scheme: light)" srcset="https://github.com/user-attachments/assets/1bb1dd23-dbc7-4a96-b556-edbae84012b5">
  <img alt="cli-header" width="100%">
</picture>

<img width="100%" alt="lean-animation" src="https://github.com/user-attachments/assets/09a32ba9-99ee-4fa9-9b33-d98dbf5d291f">

QuantConnect Lean CLI is a command-line interface tool for interacting with the Lean algorithmic trading engine, which is an open-source platform for backtesting and live trading algorithms in multiple financial markets. It allows developers to manage projects, run backtests, deploy live algorithms, and perform various other tasks related to algorithmic trading directly from the terminal. The CLI simplifies the workflow by automating tasks, enabling seamless integration with cloud services, and facilitating collaboration with the QuantConnect community. It's designed for quant developers who need a powerful and flexible tool to streamline their trading strategies. Please watch the [instructions videos](https://www.youtube.com/watch?v=QJibe1XpP-U&list=PLD7-B3LE6mz61Hojce6gKshv5-7Qo4y0r) to learn more.

### Installation

```
pip install lean
```


### Commands

Create a new project containing starter code

```
lean project-create
```

Run a local Jupyter Lab environment using Docker

```
lean research
```

Backtest a project locally using Docker

```
lean backtest
```

Optimize a project locally using Docker

```
lean optimize
```

Start live trading a project locally using Docker

```
lean live
```

Download the [LEAN CLI Cheat Sheet](https://cdn.quantconnect.com/i/tu/cli-cheat-sheet.pdf) for the full list of commands.

#

<picture >
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/85b548f8-9fd1-47f1-9b10-d73b3cfc6b23">
  <source media="(prefers-color-scheme: light)" srcset="https://github.com/user-attachments/assets/b6866983-adac-4461-ac2f-8642a72ef2a5">
  <img alt="modular-architecture" width="100%">
</picture>
<br>

![diagram](https://github.com/user-attachments/assets/f482fae4-5908-4d95-a427-4b1d685c355c)

#

<picture >
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/7b230a0d-6bf2-45bb-872e-c0faf4f1471e">
  <source media="(prefers-color-scheme: light)" srcset="https://github.com/user-attachments/assets/23b59138-aab5-43c3-91b0-20eff46ab21a">
  <img alt="modular-architecture" width="100%">
</picture>


This section will cover how to install lean locally for you to use in your environment. **For most users we strongly recommend the LEAN CLI which is prebuilt and runs on all platforms.** Refer to the following readme files for a detailed guide regarding using your local IDE with Lean.
<br/>

* [VS Code](.vscode/readme.md)
* [VS](.vs/readme.md)
  
To install locally, download the zip file with the [latest master](https://github.com/QuantConnect/Lean/archive/master.zip) and unzip it to your favorite location. Alternatively, install [Git](https://git-scm.com/downloads) and clone the repo:

```
git clone https://github.com/QuantConnect/Lean.git
cd Lean
```

### macOS 

NOTE: Visual Studio for Mac [has been discontinued](https://learn.microsoft.com/en-gb/visualstudio/releases/2022/what-happened-to-vs-for-mac), use Visual Studio Code instead

- Install [Visual Studio Code for Mac](https://code.visualstudio.com/download)
- Install the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
- Install [dotnet 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0):
- To build the solution, either:
  - choose **Run Task** > **build** from the Panel task dropdown, or
  - from the command line run
    ```
    dotnet build
    ```
- To run the solution, either:
  - choose **Run and Debug** from the Activity Bar, then click **Launch**, or
  - click F5, or
  - from the command line run
    ```
    cd Launcher/bin/Debug
    dotnet QuantConnect.Lean.Launcher.dll
    ```

### Linux (Debian, Ubuntu)

- Install [dotnet 9](https://docs.microsoft.com/en-us/dotnet/core/install/linux):
- Compile Lean Solution:
```
dotnet build QuantConnect.Lean.sln
```
- Run Lean:
```
cd Launcher/bin/Debug
dotnet QuantConnect.Lean.Launcher.dll
```

### Windows

- Install [Visual Studio](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx)
- Open `QuantConnect.Lean.sln` in Visual Studio
- Build the solution by clicking Build Menu -> Build Solution (this should trigger the NuGet package restore)
- Press `F5` to run

### Python Support

A full explanation of the Python installation process can be found in the [Algorithm.Python](https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#quantconnect-python-algorithm-project) project.

### Local-Cloud Hybrid Development. 

Seamlessly develop locally in your favorite development environment, with full autocomplete and debugging support to quickly and easily identify problems with your strategy. Please see the [CLI Home](https://www.lean.io/cli) for more information.

## Issues and Feature Requests ##

Please submit bugs and feature requests as an issue to the [Lean Repository][5]. Before submitting an issue, please read the instructions to ensure it is not duplicated.

## Mailing List ## 

The mailing list for the project can be found on [LEAN Forum][6]. Please use this to ask for assistance with your installation and setup questions.

## Contributors and Pull Requests ##

Contributions are warmly welcomed, but we ask you to read the existing code to see how it is formatted and commented on and ensure contributions match the existing style. All code submissions must include accompanying tests. Please see the [contributor guidelines][7]. All accepted pull requests will get a $50 cloud credit on QuantConnect. Once your pull request has been merged, write to us at support@quantconnect.com with a link to your PR to claim your free live trading. QC <3 Open Source.

A huge thank you to all our contributors!

<br/>

<a href="https://github.com/QuantConnect/Lean/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=QuantConnect/Lean" />
</a>

## Acknowledgements ##

The open sourcing of QuantConnect would not have been possible without the support of the Pioneers. The Pioneers formed the core 100 early adopters of QuantConnect who subscribed and allowed us to launch the project into open source. 

Ryan H, Pravin B, Jimmie B, Nick C, Sam C, Mattias S, Michael H, Mark M, Madhan, Paul R, Nik M, Scott Y, BinaryExecutor.com, Tadas T, Matt B, Binumon P, Zyron, Mike O, TC, Luigi, Lester Z, Andreas H, Eugene K, Hugo P, Robert N, Christofer O, Ramesh L, Nicholas S, Jonathan E, Marc R, Raghav N, Marcus, Hakan D, Sergey M, Peter McE, Jim M, INTJCapital.com, Richard E, Dominik, John L, H. Orlandella, Stephen L, Risto K, E.Subasi, Peter W, Hui Z, Ross F, Archibald112, MooMooForex.com, Jae S, Eric S, Marco D, Jerome B, James B. Crocker, David Lypka, Edward T, Charlie Guse, Thomas D, Jordan I, Mark S, Bengt K, Marc D, Al C, Jan W, Ero C, Eranmn, Mitchell S, Helmuth V, Michael M, Jeremy P, PVS78, Ross D, Sergey K, John Grover, Fahiz Y, George L.Z., Craig E, Sean S, Brad G, Dennis H, Camila C, Egor U, David T, Cameron W, Napoleon Hernandez, Keeshen A, Daniel E, Daniel H, M.Patterson, Asen K, Virgil J, Balazs Trader, Stan L, Con L, Will D, Scott K, Barry K, Pawel D, S Ray, Richard C, Peter L, Thomas L., Wang H, Oliver Lee, Christian L..


  [1]: https://www.lean.io/ "Lean Open Source Home Page"
  [2]: https://www.lean.io/docs/ "Lean Documentation"
  [3]: https://github.com/QuantConnect/Lean/archive/master.zip
  [4]: https://www.quantconnect.com "QuantConnect"
  [5]: https://github.com/QuantConnect/Lean/issues
  [6]: https://www.quantconnect.com/forum/discussions/1/lean
  [7]: https://github.com/QuantConnect/Lean/blob/master/CONTRIBUTING.md
  [8]: https://hub.docker.com/orgs/quantconnect/repositories
  [9]: https://www.nuget.org/profiles/jaredbroad
