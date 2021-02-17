# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

COPY NuGet.config .
COPY src/*.csproj .
RUN dotnet restore

# copy everything else and build app
COPY src .
RUN dotnet publish -c Debug -o /app --no-restore

# final stage/image
FROM cphillips83/lean:1.0.3
WORKDIR /app
COPY --from=build /app ./
COPY config.json ./
ENTRYPOINT ["dotnet", "../lean/QuantConnect.Lean.Launcher.dll"]