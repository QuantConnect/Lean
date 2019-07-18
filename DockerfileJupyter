#
#    LEAN Jupyter Docker Container 20190206
#

# Use base system for cleaning up wayward processes
FROM quantconnect/lean:foundation

MAINTAINER QuantConnect <contact@quantconnect.com>

# Install Tini
RUN wget --quiet https://github.com/krallin/tini/releases/download/v0.10.0/tini && \
    echo "1361527f39190a7338a0b434bd8c88ff7233ce7b9a4876f3315c22fce7eca1b0 *tini" | sha256sum -c - && \
    mv tini /usr/local/bin/tini && \
    chmod +x /usr/local/bin/tini

RUN git clone https://github.com/QuantConnect/pythonnet && \
    cd pythonnet && cp src/runtime/interop36.cs src/runtime/interop36m.cs && \
    python setup.py install && cd .. && rm -irf pythonnet

# Install Lean/PythonToolbox
RUN git clone https://github.com/QuantConnect/Lean.git && cd Lean/PythonToolbox && \
    python setup.py install && cd ../.. && rm -irf Lean

#Install Jupyter and other packages
RUN conda update -y conda && \
    conda install -c conda-forge jupyterlab

# Be sure packages are the same of Foundation
RUN conda install -y python=3.6.7 && \
    conda install -y numpy=1.14.5 && \
    conda install -y pandas=0.23.4 && \
    conda clean -y --all

#Install ICSharp (Jupyter C# Kernel)
RUN wget https://cdn.quantconnect.com/icsharp/ICSharp.Kernel.20180820.zip && \
    unzip ICSharp.Kernel.20180820.zip && rm -irf ICSharp.Kernel.20180820.zip && cd icsharp && \
    jupyter kernelspec install kernel-spec --name=csharp && cd ..

# Setting some environment variables
ENV WORK /root/Lean/Launcher/bin/Debug/
ENV PYTHONPATH=${WORK}:${PYTHONPATH}

# Copy Lean files to convenient locations
COPY ./Launcher/bin/Debug/ ${WORK}
RUN mkdir ${WORK}/pythonnet && \
    mv ${WORK}/decimal.py ${WORK}/pythonnet/decimal.py && \
    mv ${WORK}/nPython.exe ${WORK}/pythonnet/nPython.exe && \
    mv ${WORK}/Python.Runtime.dll ${WORK}/pythonnet/Python.Runtime.dll && \
    find ${WORK} -type f -not -name '*.dll' -not -name '*.ipynb' -not -name '*.csx' -delete && \
    echo "{ \"data-folder\": \"/home/Data/\", \"composer-dll-directory\": \"$WORK\" }" > ${WORK}/config.json

EXPOSE 8888
WORKDIR $WORK

ENTRYPOINT [ "/usr/local/bin/tini", "--" ]
CMD jupyter lab --ip='0.0.0.0' --port=8888 --no-browser --allow-root

# List packages
RUN conda list

# Usage:
# docker build -t quantconnect/lean:foundation -f DockerfileLeanFoundation .
# docker build -t quantconnect/jupyter -f DockerfileJupyter .
# docker run -it --rm -p 8888:8888 -v (absolute to your data folder):/home/Data:ro quantconnect/jupyter