#
#   LEAN Docker Container - ECS Live Trading Edition
#   Custom build with Tradovate brokerage and SNS notifications
#

# Use base system
FROM quantconnect/lean:foundation

MAINTAINER QuantConnect <contact@quantconnect.com>

# Install debugpy and PyDevD for remote python debugging
RUN pip install --no-cache-dir ptvsd==4.3.2 debugpy~=1.6.7 pydevd-pycharm~=231.9225.15

# Install AWS SDK for SNS notifications and AWS CLI for S3 sync
RUN pip install --no-cache-dir boto3 awscli

# Install vsdbg for remote C# debugging in Visual Studio and Visual Studio Code
RUN wget https://aka.ms/getvsdbgsh -O - 2>/dev/null | /bin/sh /dev/stdin -v 17.10.20209.7 -l /root/vsdbg

# Copy custom plugins (DataBento, Tradovate, etc.)
COPY ./DataLibraries /Lean/Launcher/bin/Debug/

# Copy essential Data files (symbol properties, market hours - required for live trading)
# Full market data omitted for size
COPY ./Data/symbol-properties/ /Lean/Data/symbol-properties/
COPY ./Data/market-hours/ /Lean/Data/market-hours/

# Copy LEAN engine binaries
COPY ./Launcher/bin/Debug/ /Lean/Launcher/bin/Debug/
COPY ./Optimizer.Launcher/bin/Debug/ /Lean/Optimizer.Launcher/bin/Debug/
COPY ./Report/bin/Debug/ /Lean/Report/bin/Debug/
COPY ./DownloaderDataProvider/bin/Debug/ /Lean/DownloaderDataProvider/bin/Debug/

# Copy config template and entrypoint script
COPY config-template.json /config-template.json
COPY scripts/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Create directories for algorithm and results
# /LeanCLI will be mounted from EFS at runtime (allows updates without image rebuild)
# /Results will be mounted from EFS for persistent output
RUN mkdir -p /LeanCLI /Results

# Working directory
WORKDIR /Lean/Launcher/bin/Debug

# Use wrapper entrypoint that handles credential injection
ENTRYPOINT ["/docker-entrypoint.sh"]
