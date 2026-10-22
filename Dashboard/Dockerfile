# Build from the repository root: docker compose -f Dashboard/docker-compose.yml up --build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build Launcher/QuantConnect.Lean.Launcher.csproj -c Debug -v q \
 && grep -rhoE 'class \w+ : QCAlgorithm\b' Algorithm.CSharp | awk '{print $2}' | sort -u > /src/algorithms.txt

FROM mcr.microsoft.com/dotnet/runtime:10.0
RUN apt-get update \
 && apt-get install -y --no-install-recommends python3 python3-venv \
 && rm -rf /var/lib/apt/lists/*
COPY Dashboard/requirements.txt /app/requirements.txt
RUN python3 -m venv /venv && /venv/bin/pip install --no-cache-dir -r /app/requirements.txt

COPY --from=build /src/Launcher/bin/Debug /Lean/Launcher/bin/Debug
COPY --from=build /src/algorithms.txt /Lean/algorithms.txt
COPY Dashboard/server.py Dashboard/index.html /app/

WORKDIR /app
EXPOSE 8000
CMD ["/venv/bin/uvicorn", "server:app", "--host", "0.0.0.0", "--port", "8000"]
