# Use Lean Foundation as the base
FROM quantconnect/lean:foundation

# Install docker to enable access to local docker installation
RUN curl -fsSL https://download.docker.com/linux/ubuntu/gpg | apt-key add - && \
    add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" && \
    apt-get update && \
    apt-get install -y docker-ce

# Update git to latest, enables features such as stashing individual files
RUN add-apt-repository -y ppa:git-core/ppa && \
    apt-get update && \
    apt-get install -y git

# Install Python Tool for Visual Studio Debugger for remote python debugging
RUN pip install ptvsd