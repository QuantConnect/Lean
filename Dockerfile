#
#   LEAN Docker Container 20200522
#   Cross platform deployment for multiple brokerages
#

# Use base system
FROM quantconnect/lean:foundation

MAINTAINER QuantConnect <contact@quantconnect.com>

#Install Python Tool for Visual Studio Debugger for remote python debugging
RUN pip install ptvsd

#Install PyDev Debugger for Pycharm for remote python debugging
RUN pip install pydevd-pycharm~=201.8538.36

COPY ./Launcher/bin/Debug/ /Lean/Launcher/bin/Debug/

# Can override with '-w'
WORKDIR /Lean/Launcher/bin/Debug

ENTRYPOINT [ "mono", "QuantConnect.Lean.Launcher.exe" ]
