# Script for building the project file tar
#find . -name "*.csproj" | tar -cf projectfiles.tar.gz -T -

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

COPY *.sln ./
COPY Directory.Build.props .
ADD projectfiles.tar.gz ./
RUN find . -type f -name '*.csproj' -exec dotnet restore "{}" \;
#RUN dotnet restore

# copy everything else and build app
COPY . .
RUN dotnet publish Launcher/QuantConnect.Lean.Launcher.csproj -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:5.0
WORKDIR /app
COPY --from=build /app ./
COPY --from=build /source/Data /Data
ENTRYPOINT ["dotnet", "QuantConnect.Lean.Launcher.dll"]