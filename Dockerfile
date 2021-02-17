# Script for building the project file tar
#find . -name "*.sln" | tar -cf Dockerfiles/sln.tar.gz -T -
#find . -name "*.fsproj" | tar -cf Dockerfiles/fsproj.tar.gz -T -
#find . -name "*.csproj" | tar -cf Dockerfiles/csproj.tar.gz -T -

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

COPY Directory.Build.props .
ADD Dockerfiles/*.tar.gz ./
RUN find . -type f -name '*.sln' -exec dotnet restore "{}" \;

# copy everything else and build app
COPY . .
RUN dotnet publish Launcher/QuantConnect.Lean.Launcher.csproj -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:5.0
WORKDIR /app
COPY --from=build /app ./
COPY --from=build /source/Data /Data
ENTRYPOINT ["dotnet", "QuantConnect.Lean.Launcher.dll"]