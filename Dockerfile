FROM mono:4.0.5.1

RUN mozroots --import --sync

RUN mkdir -p /usr/src/app/source /usr/src/app/build
WORKDIR /usr/src/app/source

COPY . /usr/src/app/source
RUN sed -i 's/4.5/4.0/' Algorithm.VisualBasic/QuantConnect.Algorithm.VisualBasic.vbproj
RUN nuget restore QuantConnect.Lean.sln -NonInteractive
RUN xbuild /property:Configuration=Release
WORKDIR /usr/src/app/source/Launcher/bin/Release/

CMD [ "mono", "./QuantConnect.Lean.Launcher.exe"] # Run app

# usage:
# docker build -t lean .
# docker run lean
