![alt tag](Documentation/logo.white.small.png)
Lean C# Algorithmic Trading Engine
=========

[![Join the chat at https://quantconnect-slack.herokuapp.com](https://cdn.quantconnect.com/lean/i/slack-sm.png)](https://quantconnect-slack.herokuapp.com/) &nbsp;&nbsp;&nbsp;&nbsp; <img src="https://travis-ci.org/QuantConnect/Lean.svg?branch=master">  &nbsp;&nbsp;&nbsp;&nbsp;  [![Coverage Status](https://coveralls.io/repos/QuantConnect/Lean/badge.svg?branch=master&service=github)](https://coveralls.io/github/QuantConnect/Lean?branch=master)

[Lean Home - lean.quantconnect.com][1] | [Documentation][2] | [Download Zip][3]

----------

## Introduction ##

Lean Engine is an open-source fully managed C# algorithmic trading engine built for desktop and cloud usage. It was designed in Mono and operates in Windows, Linux and Mac platforms. The community has contributed additional connectors to F#, Visual Basic and Java.

Lean drives the web based backtesting platform [QuantConnect][4].

## System Overview ##

Lean outsourced key infrastructure management to plugins. The most important plugins are:

 - **Result Processing**
   > Handle all messages from the algorithmic trading engine. Decide what should be sent, and where the messages should go. The result processing system can send messages to a local GUI, or the web interface.

 - **Datafeed Sourcing**
   > Connect and download data required for the algorithmic trading engine. For backtesting this sources files from the disk, for live trading it connects to a stream and generates the data objects.

 - **Transaction Processing**
   > Process new order requests; either using the fill models provided by the algorithm, or with an actual brokerage. Send the processed orders back to the algorithm's portfolio to be filled.

 - **Realtime Event Management**
   > Generate real time events - such as end of day events. Trigger callbacks to real time event handlers. For backtesting this is mocked-up an works on simulated time. 
 
 - **Algorithm State Setup**
   > Configure the algorithm cash, portfolio and data requested. Initialize all state parameters required.

For more information on the system design and contributing please see the Lean Website Documentation.

## Spinup Instructions ##

### OS X

Install [Mono for Mac](http://www.mono-project.com/docs/getting-started/install/mac/)

Install [MonoDevelop](http://www.monodevelop.com/download/) or [Xamarin Studio](http://xamarin.com/studio) for your IDE. If you use MonoDevelop also install its [FSharp Plugin](http://addins.monodevelop.com/Project/Index/48).

Clone the repo:
```
git clone git@github.com:QuantConnect/Lean.git
cd Lean
```

OSX does not fully support Visual Basic or F#. You will need to remove these projects from the solution for them to build properly. Alternatively for Visual Basic modify the target framework as shown [here](https://groups.google.com/forum/#!topic/lean-engine/uR94evlM01g). Alternatively modify the target framework:
```
sed -i -e 's/4.5/4.0/' Algorithm.VisualBasic/QuantConnect.Algorithm.VisualBasic.vbproj
```

Open the project in Xamarin Studio, then in the menu bar, click `Project > Update NuGet Packages`. You should also run `nuget install MathNet.Filtering -pre` to install the MathNet library. 

In OS X `mdtool` is not added to the PATH environment. Either set up the PATH manually or reference the binary directly.

If you are running Xamarin Studio:
```
/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool build
```

If you are running MonoDevelop:
```
/Applications/MonoDevelop.app/Contents/MacOS/mdtool build
```

Run the compiled `exe` file. For the time being you need to run the `exe` in the same path as your current working directory:
```
cd Lean/Launcher/bin/Debug
mono ./QuantConnect.Lean.Launcher.exe
```
### Linux (Debian, Ubuntu)

Setup Mono GPG signing key ([instructions here](http://www.mono-project.com/docs/getting-started/install/linux/)).

Install dependencies, MonoDevelop, Git and NuGet:
```
sudo apt-get install mono-complete mono-vbnc fsharp monodevelop monodevelop-nunit  git ca-certificates-mono
mozroots --import --sync
apt-get upgrade mono-complete
```
Clone the repo:
```
git clone https://github.com/QuantConnect/Lean.git
cd Lean
```
Like OSX, Linux does not fully support Visual Basic. You will need to remove this project from the solution for them to build properly. Alternatively modify the target framework:
```
sed -i 's/4.5/4.0/' Algorithm.VisualBasic/QuantConnect.Algorithm.VisualBasic.vbproj
```
Restore NuGet packages then compile:
```
wget https://nuget.org/nuget.exe
mono nuget.exe restore QuantConnect.Lean.sln
xbuild
```
If you get: "Error initializing task Fsc: Not registered task Fsc." -> apt-get upgrade mono-complete
If you get: "XX not found" -> Make sure Nuget ran successfully, and re-run if neccessary.

Run the compiled `exe` file. For the time being you need to run the `exe` in the same path as your current working directory:
```
cd Lean/Launcher/bin/Debug
./QuantConnect.Lean.Launcher.exe
```
### Windows

- Install [Visual Studio](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx)
- Open `QuantConnect.Lean.sln` in Visual Studio
- Press `ctrl-f5` to run without debugging.
By default Visual Studio includes NuGet, if your version cannot find DLL references, install [Nuget](https://www.nuget.org/) and build again. 


## Issues and Feature Requests ##

Please submit bugs and feature requests as an issue to the [Lean Repository][5]. Before submitting an issue please read others to ensure it is not a duplicate.

## Mailing List ##

The mailing list for the project can be found on [Google Groups][6]

## Contributors and Pull Requests ##

Contributions are warmly very welcomed but we ask you read the existing code to see how it is formatted, commented and ensure contributions match the existing style. All code submissions must include accompanying tests. Please see the [contributor guide lines][7].

## Build Status ##
<img src="https://travis-ci.org/QuantConnect/Lean.svg?branch=master">

## Acknowledgements ##

The open sourcing of QuantConnect would not have been possible without the support of the Pioneers. The Pioneers formed the core 100 early adopters of QuantConnect who subscribed and allowed us to launch the project into open source.

Ryan H, Pravin B, Jimmie B, Nick C, Sam C, Mattias S, Michael H, Mark M, Madhan, Paul R, Nik M, Scott Y, BinaryExecutor.com, Tadas T, Matt B, Binumon P, Zyron, Mike O, TC, Luigi, Lester Z, Andreas H, Eugene K, Hugo P, Robert N, Christofer O, Ramesh L, Nicholas S, Jonathan E, Marc R, Raghav N, Marcus, Hakan D, Sergey M, Peter McE, Jim M, INTJCapital.com, Richard E, Dominik, John L, H. Orlandella, Stephen L, Risto K, E.Subasi, Peter W, Hui Z, Ross F, Archibald112, MooMooForex.com, Jae S, Eric S, Marco D, Jerome B, James B. Crocker, David Lypka, Edward T, Charlie Guse, Thomas D, Jordan I, Mark S, Bengt K, Marc D, Al C, Jan W, Ero C, Eranmn, Mitchell S, Helmuth V, Michael M, Jeremy P, PVS78, Ross D, Sergey K, John Grover, Fahiz Y, George L.Z., Craig E, Sean S, Brad G, Dennis H, Camila C, Egor U, David T, Cameron W, Napoleon Hernandez, Keeshen A, Daniel E, Daniel H, M.Patterson, Asen K, Virgil J, Balazs Trader, Stan L, Con L, Will D, Scott K, Barry K, Pawel D, S Ray, Richard C, Peter L, Thomas L., Wang H, Oliver Lee, Christian L.


  [1]: https://lean.quantconnect.com "Lean Open Source Home Page"
  [2]: https://lean.quantconnect.com/docs "Lean Documentation"
  [3]: https://github.com/QuantConnect/Lean/archive/master.zip
  [4]: https://www.quantconnect.com "QuantConnect"
  [5]: https://github.com/QuantConnect/Lean/issues
  [6]: https://groups.google.com/forum/#!forum/lean-engine
  [7]: https://github.com/QuantConnect/Lean/blob/master/CONTRIBUTING.md
