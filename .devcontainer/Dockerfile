# Use Lean Foundation as the base
FROM quantconnect/lean:foundation

# Update git to latest, enables features such as stashing individual files
RUN add-apt-repository -y ppa:git-core/ppa && \
    apt-get update && \
    apt-get install -y git

# Install useful pip packages
RUN pip install --no-cache-dir      \
    ptvsd                           \
    quantconnect-stubs

# Install dos2unix utility for converting pesky windows formatting when needed
RUN apt-get install -y dos2unix

# ------ Research Specific Setup ------
# Install clr-loader for running Py research in VS code
RUN pip install clr-loader==0.1.6

ENV LD_LIBRARY_PATH=/lib/
RUN ln -s /lib/$(uname -m)-linux-gnu/libc.so.6 /lib/$(uname -m)-linux-gnu/libc.so

# Install .NET Interactive to support C# in Jupyter notebooks
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install -g --add-source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" Microsoft.dotnet-interactive && \
    dotnet interactive jupyter install