# Lean Container

Custom LEAN Docker image with modified data sources for Cascade Labs.

## Overview

This project builds a custom LEAN Docker image that includes:
- Modified ThetaData provider with Bearer token authentication
- TradeAlert data providers for options flow data (sweeps, most_active, snapshot)
- Local caching of market data after initial fetch
- No dependency on QuantConnect module downloads

## Setup

### 1. Clone LEAN Repository

```bash
cd lean_container
git clone https://github.com/QuantConnect/Lean.git
```

### 2. Clone ThetaData Data Source (for reference)

```bash
cd Lean
git clone https://github.com/QuantConnect/Lean.DataSource.ThetaData.git
```

### 3. Copy Our Modified Data Source

```bash
# Copy CascadeThetaData into LEAN's data source location
cp -r ../data_sources/CascadeThetaData ./Lean.DataSource.CascadeThetaData
```

### 4. Add Project Reference to LEAN

Edit `Lean/Launcher/QuantConnect.Lean.Launcher.csproj` and add:

```xml
<ItemGroup>
  <ProjectReference Include="..\Lean.DataSource.CascadeThetaData\CascadeThetaData.csproj" />
</ItemGroup>
```

### 5. Build the Custom Image

From the `lean_container` directory (parent of `Lean`):

```bash
lean build --tag cascadelabs/lean:latest
```

This will:
- Build the foundation image (or use official if unchanged)
- Compile all C# code including CascadeThetaData
- Build the engine image
- Build the research image

### 6. Configure lean-cli to Use Custom Image

```bash
lean config set engine-image cascadelabs/lean:latest
```

## Directory Structure

```
lean_container/
├── README.md
├── Lean/                              # Cloned LEAN repository
│   ├── Launcher/
│   ├── Engine/
│   ├── Common/
│   ├── Lean.DataSource.ThetaData/         # Cloned ThetaData (reference)
│   ├── Lean.DataSource.CascadeThetaData/  # Our modified version (copied)
│   └── Lean.DataSource.CascadeTradeAlert/ # TradeAlert providers (copied)
├── data_sources/
│   ├── CascadeThetaData/              # Source of our ThetaData modifications
│   │   ├── CascadeThetaDataProvider.cs
│   │   ├── CascadeThetaDataRestClient.cs
│   │   └── CascadeDataCache.cs
│   └── CascadeTradeAlert/             # TradeAlert data providers
│       ├── TradeAlertSweepsProvider.cs
│       ├── TradeAlertMostActiveProvider.cs
│       ├── TradeAlertSnapshotProvider.cs
│       ├── TradeAlertBaseProvider.cs
│       ├── TradeAlertPathUtils.cs
│       └── S3TradeAlertClient.cs
└── scripts/
    ├── setup.sh                       # Clone and setup repos
    └── build.sh                       # Build custom image
```

## Key Modifications from Stock ThetaData

### 1. Bearer Token Authentication
The stock ThetaData provider expects a local ThetaData Terminal. Our version:
- Reads `thetadata-auth-token` from config
- Adds `Authorization: Bearer <token>` header to all REST requests
- Connects directly to `thetadata.cascadelabs.io`

### 2. Remove QC Subscription Validation
The stock provider calls QuantConnect's API to validate subscriptions. We remove this
since we're using our own deployment.

### 3. Session Caching
Within a single backtest session:
- In-memory cache prevents duplicate API calls for the same data
- LEAN handles persistent data caching in `/Lean/Data` after conversion

## Configuration

### ThetaData Configuration

The following config keys are used for ThetaData:

| Key | Description | Example |
|-----|-------------|---------|
| `thetadata-rest-url` | ThetaData REST API endpoint | `https://thetadata.cascadelabs.io` |
| `thetadata-auth-token` | Bearer token for authentication | `your-api-key` |
| `thetadata-subscription-plan` | Subscription tier | `Pro` |

### TradeAlert Configuration

The following config keys are used for TradeAlert S3 access:

| Key | Description | Example |
|-----|-------------|---------|
| `s3-endpoint` | S3-compatible endpoint (OCI) | `objectstorage.us-ashburn-1.oraclecloud.com` |
| `tradealert-s3-bucket` | S3 bucket name | `cascadelabs-data` |
| `s3-region` | S3 region | `us-ashburn-1` |
| `s3-access-key` | S3 access key | `your-access-key` |
| `s3-secret-key` | S3 secret key | `your-secret-key` |

## TradeAlert Data Providers

Three independent providers are available for querying TradeAlert data from S3:

### TradeAlertSweepsProvider

Option sweeps/block trades data (5-minute intervals).

```csharp
var sweepsProvider = new TradeAlertSweepsProvider();

// Get sweeps for a specific timestamp
var sweeps = sweepsProvider.GetData(DateTime.UtcNow);

// Filter by minimum contract size
var largeSweeps = sweepsProvider.GetDataBySize(timestamp, minSize: 100);

// Get call sweeps only
var callSweeps = sweepsProvider.GetCallSweeps(timestamp);

// Filter by delta range
var deltaSweeps = sweepsProvider.GetDataByDelta(timestamp, minDelta: 0.3, maxDelta: 0.7);
```

### TradeAlertMostActiveProvider

Most active underlyings by options volume (5-minute intervals).

```csharp
var mostActiveProvider = new TradeAlertMostActiveProvider();

// Get most active stocks
var mostActive = mostActiveProvider.GetData(DateTime.UtcNow);

// Get top 10 by volume
var top10 = mostActiveProvider.GetTopN(timestamp, topN: 10);

// Filter by IV percentile
var highIv = mostActiveProvider.GetByIvPercentile(timestamp, minIvPctl: 80, maxIvPctl: 100);

// Get stocks with upcoming earnings
var earnings = mostActiveProvider.GetWithUpcomingEarnings(timestamp, maxDaysToEarnings: 5);
```

### TradeAlertSnapshotProvider

End-of-day (EOD) snapshot data for all underlyings (daily at market close).

```csharp
var snapshotProvider = new TradeAlertSnapshotProvider();

// Get EOD snapshot for a date
var eodData = snapshotProvider.GetData(date);

// Get date range
var rangeData = snapshotProvider.GetDataRange(startDate, endDate);

// Filter by sector
var techStocks = snapshotProvider.GetBySector(date, "Technology");

// Get stocks at IV extremes
var ivExtremes = snapshotProvider.GetAtIvExtremes(date, nearHighOrLow: true);

// Get hard-to-borrow stocks
var htb = snapshotProvider.GetHardToBorrow(date, minBorrowRate: 10);
```

## Usage

After building and configuring, run backtests normally:

```bash
lean backtest MyProject
```

The custom image will automatically use CascadeThetaData when configured.

## Private Container Registry (GHCR)

The Lean container is automatically built and published to GitHub Container Registry (GHCR) when changes are pushed to `main`.

### Published Images

- **Engine**: `ghcr.io/cascade-labs/cascadelabs-lean/engine:latest`
- **Research**: `ghcr.io/cascade-labs/cascadelabs-lean/research:latest`

### Authenticating with GHCR

To pull the private images, you need to authenticate with GitHub Container Registry:

#### Option 1: Using a Personal Access Token (recommended for local development)

1. Create a Personal Access Token (PAT) at https://github.com/settings/tokens
   - Select scope: `read:packages`

2. Log in to GHCR:
   ```bash
   echo $GITHUB_TOKEN | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
   ```

   Or with Podman:
   ```bash
   echo $GITHUB_TOKEN | podman login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
   ```

#### Option 2: Using GitHub CLI (simplest)

```bash
gh auth login
gh auth token | docker login ghcr.io -u $(gh api user -q .login) --password-stdin
```

### Pulling the Image

Once authenticated:

```bash
docker pull ghcr.io/cascade-labs/cascadelabs-lean/engine:latest
```

### Using with lean-cli

Configure lean-cli to use the GHCR image:

```bash
lean config set engine-image ghcr.io/cascade-labs/cascadelabs-lean/engine:latest
lean config set research-image ghcr.io/cascade-labs/cascadelabs-lean/research:latest
```

### Using with lean_worker

Update the `lean_image` parameter in `ContainerRunner`:

```python
runner = ContainerRunner(
    lean_image="ghcr.io/cascade-labs/cascadelabs-lean/engine:latest"
)
```

### Manual Trigger

You can manually trigger a build from the GitHub Actions tab:
1. Go to Actions > "Build and Publish Lean Container"
2. Click "Run workflow"

## Updating

When LEAN releases updates:

```bash
cd Lean
git pull origin master
cd ..
lean build --tag cascadelabs/lean:latest
```

## Future Data Sources

This project is structured to support additional data sources:
- Add new sources to `data_sources/`
- Copy to `Lean/Lean.DataSource.*/`
- Add project reference to Launcher
- Rebuild with `lean build`
