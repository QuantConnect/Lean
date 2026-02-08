# AGENTS.md - Cascade LEAN Fork

This is the Cascade Labs fork of QuantConnect LEAN with custom data providers for multiple markets.

## Custom Data Providers

### ThetaData (Default for Equities)
- **Location:** `DataSource/CascadeThetaData/`
- **Market:** US Equities (stocks, ETFs, options)
- **Usage:** Default historical data provider for equity backtests
- **API:** WebSocket support via `thetadata.cascadelabs.io`
- **Config:**
  ```json
  "thetadata-api-key": "your-key"
  ```

### CascadeKalshiData (Prediction Markets)
- **Location:** `DataSource/CascadeKalshiData/`
- **Market:** Kalshi prediction markets
- **SecurityType:** `PredictionMarket` / `Base`
- **Usage:** Use ONLY for Kalshi market backtests (weather, events, politics)
- **API:** `https://data.cascadelabs.io/kalshi`
- **Config:**
  ```json
  "kalshi-api-key": "your-key",
  "kalshi-api-url": "https://data.cascadelabs.io/kalshi"
  ```
- **Note:** Should NOT be called for equity history requests

### CascadeHyperliquid (Crypto Futures)
- **Location:** `DataSource/CascadeHyperliquid/`
- **Market:** Hyperliquid perpetual futures
- **SecurityType:** `CryptoFuture`
- **Usage:** Crypto perps backtests and live trading
- **API:** Public Hyperliquid API (no auth required)
- **Symbols:** BTCUSD, ETHUSD, SOLUSD, etc.
- **Status:** History provider built, needs MEF registration

### CascadeTradeAlert (Custom Data Feed)
- **Location:** `DataSource/CascadeTradeAlert/`
- **Purpose:** Options flow/sweeps data for alpha signals
- **NOT a history provider** - custom data accessed via algorithm
- **Data Types:**
  - `sweeps` - Real-time options sweep alerts
  - `most_active` - Top symbols by options volume
  - `snapshot` - EOD underlying fields
- **Config:**
  ```json
  "tradealert-data-path": "/Lean/Data/tradealert",
  "s3-endpoint": "...",
  "tradealert-s3-bucket": "trade_alert",
  "s3-access-key": "...",
  "s3-secret-key": "..."
  ```
- **Local Cache:** Set `tradealert-data-path` to use local parquet files before S3

## Data Provider Routing

The `HistoryProviderManager` routes history requests to providers based on:
- SecurityType
- Market

**Current Issue:** CascadeKalshiDataProvider is incorrectly registered for all history requests. It should only handle `SecurityType.PredictionMarket` with `Market.Kalshi`.

**Correct Routing:**
| SecurityType | Market | Provider |
|--------------|--------|----------|
| Equity | usa | ThetaData (default) |
| Option | usa | ThetaData |
| PredictionMarket | kalshi | CascadeKalshiData |
| CryptoFuture | hyperliquid | CascadeHyperliquid |
| CryptoFuture | binance | Default/Binance |

## Building

```bash
# Build inside foundation container
docker run --rm -v "$PWD":/src -w /src quantconnect/lean:foundation \
  dotnet build Launcher/QuantConnect.Lean.Launcher.csproj -c Debug

# Copy to staging
cp Launcher/bin/Debug/*.dll Lean/Launcher/bin/Debug/

# Build Docker image
docker build -f Dockerfile -t lean-cli/engine:cascadelabs-lean .
```

## Docker Image

`lean-cli/engine:cascadelabs-lean` - Custom engine with all Cascade data providers

## Related Repos
- `lean-cli` - Modified for Kalshi credentials and pydantic v2
- `lean-worker` - Cloud worker with extended timeouts
