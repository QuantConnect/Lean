#
#   LEAN Docker Container 20200522
#   Cross platform deployment for multiple brokerages
#

# Use base system
FROM quantconnect/lean:foundation

MAINTAINER QuantConnect <contact@quantconnect.com>

COPY ./Launcher/bin/Debug/ /Lean/Launcher/bin/Debug/

# Can override with '-w'
WORKDIR /Lean/Launcher/bin/Debug

ENTRYPOINT [ "mono", "QuantConnect.Lean.Launcher.exe" ]