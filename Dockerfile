#
#   LEAN Docker Container 20200522
#   Cross platform deployment for multiple brokerages
#

# Use base system
FROM quantconnect/lean:foundation

MAINTAINER QuantConnect <contact@quantconnect.com>

#Install debugpy and PyDevD for remote python debugging
RUN pip install --no-cache-dir ptvsd==4.3.2 debugpy~=1.6.7 pydevd-pycharm~=231.9225.15

# Install vsdbg for remote C# debugging in Visual Studio and Visual Studio Code
RUN wget https://aka.ms/getvsdbgsh -O - 2>/dev/null | /bin/sh /dev/stdin -v 17.10.20209.7 -l /root/vsdbg

COPY ./Data/ /Lean/Data/
COPY ./Launcher/bin/Debug/ /Lean/Launcher/bin/Debug/
COPY ./Optimizer.Launcher/bin/Debug/ /Lean/Optimizer.Launcher/bin/Debug/
COPY ./Report/bin/Debug/ /Lean/Report/bin/Debug/
COPY ./DownloaderDataProvider/bin/Debug/ /Lean/DownloaderDataProvider/bin/Debug/

# Backup database files so they survive volume mounts over /Lean/Data
RUN cp -r /Lean/Data/market-hours /Lean/Data-market-hours && \
    cp -r /Lean/Data/symbol-properties /Lean/Data-symbol-properties

# Can override with '-w'
WORKDIR /Lean/Launcher/bin/Debug

# Restore database files after volume mounts, then exec LEAN
ENTRYPOINT [ "sh", "-c", "mkdir -p /Lean/Data/market-hours /Lean/Data/symbol-properties && cp -r /Lean/Data-market-hours/* /Lean/Data/market-hours/ && cp -r /Lean/Data-symbol-properties/* /Lean/Data/symbol-properties/ && exec dotnet QuantConnect.Lean.Launcher.dll \"$@\"", "--" ]
