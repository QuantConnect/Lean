# Use Lean Foundation as the base
FROM quantconnect/lean:foundation

# Update git to latest, enables features such as stashing individual files
RUN add-apt-repository -y ppa:git-core/ppa && \
    apt-get update && \
    apt-get install -y git

# Install Python Tool for Visual Studio Debugger for remote python debugging
RUN pip install ptvsd

# Install dos2unix utility for converting pesky windows formatting when needed
RUN apt-get install -y dos2unix