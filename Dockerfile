#
#	LEAN Algorithm Docker Container November-2016
#	Cross platform deployment for multiple brokerages
#

FROM quantconnect/lean:foundation

MAINTAINER QuantConnect <contact@quantconnect.com>

#################################
# Option 1: Download from Master
# RUN \
#	wget https://github.com/QuantConnect/Lean/archive/master.zip && \
#	unzip master.zip /root/Lean && \
#	cd /root/Lean
# RUN \
#	sed -i 's/4.5/4.0/' Algorithm.VisualBasic/QuantConnect.Algorithm.VisualBasic.vbproj && \
#	wget https://nuget.org/nuget.exe && \
#	mono nuget.exe restore QuantConnect.Lean.sln -NonInteractive && \
#	xbuild /property:Configuration=Release && \
#	cd /root/Lean/Launcher/bin/Release/
#################################


################################
# Option 2: Run Local Binaries:
COPY ./Launcher/bin/Release /root/Lean/Launcher/bin/Release
#################################

# Finally.
WORKDIR /root/Lean/Launcher/bin/Release
CMD [ "mono", "QuantConnect.Lean.Launcher.exe"] # Run app

# Usage: 
# docker build -t quantconnect/lean:foundation -f DockerfileLeanFoundation .
# docker run -i -t -v ~/Lean:/root/Lean/ quantconnect/lean:foundation /bin/bash
# Note: '~' means Home, for example "C:\Users\YourName\". Here we assume Lean is in "C:\Users\YourName\Lean". You could change that.
#        This command will get you inside the container 'quantconnect/lean:foundation', using '/bin/bash' as you terminal, and mapping your Lean in Windows '~/Lean' to Lean in the docker '/root/Lean/'.
# After compiling Lean, you can run it by executing ./Launcher/bin/Debug/QuantConnect.Lean.Launcher.exe

# Please clean up Docker images by the following command and rebuild the Docker in case commands did not run correctly.
# docker stop $(docker ps -a -q) && docker rm $(docker ps -a -q) && docker rmi $(docker images -a -q)

