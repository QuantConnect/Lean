#
#   LEAN Docker Container 20200522
#   Cross platform deployment for multiple brokerages
#

# Use base system
FROM quantconnect/lean:foundation

MAINTAINER QuantConnect <contact@quantconnect.com>

#Install debugpy and PyDevD for remote python debugging
RUN pip install --no-cache-dir ptvsd==4.3.2 debugpy~=1.5.1 pydevd-pycharm~=201.8538.36

# Install vsdbg for remote C# debugging in Visual Studio and Visual Studio Code
RUN wget https://aka.ms/getvsdbgsh -O - 2>/dev/null | /bin/sh /dev/stdin -v 16.9.20122.2 -l /root/vsdbg

COPY ./DataLibraries /Lean/Launcher/bin/Debug/
COPY ./Lean/Launcher/bin/Debug/ /Lean/Launcher/bin/Debug/
COPY ./Lean/Optimizer.Launcher/bin/Debug/ /Lean/Optimizer.Launcher/bin/Debug/
COPY ./Lean/Report/bin/Debug/ /Lean/Report/bin/Debug/

# Can override with '-w'
WORKDIR /Lean/Launcher/bin/Debug

ENTRYPOINT [ "dotnet", "QuantConnect.Lean.Launcher.dll" ]
