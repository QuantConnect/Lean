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
RUN pip install clr-loader

#Install ICSharp (Jupyter C# Kernel)
RUN wget https://cdn.quantconnect.com/icsharp/ICSharp.Kernel.20180820.zip && \
    unzip ICSharp.Kernel.20180820.zip -d / && rm -irf ICSharp.Kernel.20180820.zip && cd /icsharp && \
    jupyter kernelspec install kernel-spec --name=csharp
