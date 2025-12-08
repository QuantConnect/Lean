# CONVENTIONS.md - Naming Rules & Standards

> **The single source of truth for all naming conventions in this project.**
> Follow these rules exactly. No exceptions.

---

## Why Conventions Matter

1. **AI Consistency** - Claude can produce consistent code across sessions
2. **Findability** - You can locate any file without guessing
3. **History** - Understand what something is from its name alone
4. **No Debugging** - Fewer errors from inconsistent naming

---

## 1. Directory Naming

### Pattern: `lowercase-with-dashes`

```
GOOD:
  uw-alert-straddle/
  iron-condor-weekly/
  momentum-scalp/

BAD:
  UW_Alert_Straddle/
  uwAlertStraddle/
  uw alert straddle/
```

### Reserved Directory Names

| Name | Purpose | Location |
|------|---------|----------|
| `active/` | Strategies in development/testing | `/Strategies/active/` |
| `live/` | Deployed to real money | `/Strategies/live/` |
| `paper/` | Paper trading | `/Strategies/paper/` |
| `archive/` | Retired strategies | `/Strategies/archive/` |
| `_templates/` | Reusable templates | `/Strategies/_templates/` |
| `_analytics/` | Shared analysis tools | `/Strategies/_analytics/` |
| `_data/` | Shared data/database | `/Strategies/_data/` |
| `versions/` | Previous strategy iterations | Within each strategy |
| `backtests/` | Backtest results | Within each strategy |

---

## 2. File Naming

### Python Files: `snake_case.py`

```
GOOD:
  algorithm.py
  trade_analyzer.py
  monthly_report.py
  gex_calculator.py

BAD:
  Algorithm.py
  TradeAnalyzer.py
  monthly-report.py
```

### Standard File Names (Always Use These)

| File | Purpose | Required? |
|------|---------|-----------|
| `STRATEGY.md` | Strategy specification | Yes |
| `algorithm.py` | Main algorithm code | Yes |
| `config.json` | Strategy parameters | Yes |
| `CHANGELOG.md` | Version history | Yes |
| `RESEARCH.md` | Link to research/findings | Optional |
| `README.md` | Directory explanation | In shared dirs |

### Markdown Files: `UPPER_CASE.md` for Important Docs

```
GOOD:
  STRATEGY.md      (strategy spec)
  CHANGELOG.md     (version history)
  README.md        (directory info)
  RESEARCH.md      (research notes)
  CLAUDE.md        (AI memory)
  CONVENTIONS.md   (this file)
  WORKFLOW.md      (process docs)

BAD:
  strategy.md
  Strategy.md
  strategy-spec.md
```

### Config Files: Always `config.json`

```
GOOD:
  config.json

BAD:
  Config.json
  settings.json
  params.json
  strategy_config.json
```

---

## 3. Version Naming

### Pattern: `v{N}_{YYYY-MM-DD}.py`

```
GOOD:
  v1_2024-12-01.py
  v2_2024-12-08.py
  v8_2024-12-06.py   (matching your V8.2 research)

BAD:
  v1.py
  version_1.py
  algorithm_v1.py
  2024-12-01_v1.py
```

### Rules
- `N` = major version number (matches your research versions)
- Date = when this version was created/frozen
- Store in `versions/` directory within strategy folder

---

## 4. Backtest Result Naming

### Directory Pattern: `YYYY-MM-DD/`

```
backtests/
├── 2024-12-01/
│   ├── summary.json
│   ├── trades.csv
│   └── report.html
├── 2024-12-08/
│   ├── summary.json
│   ├── trades.csv
│   └── report.html
```

### If Multiple Backtests Same Day: `YYYY-MM-DD-{N}/`

```
backtests/
├── 2024-12-08-1/   (first run)
├── 2024-12-08-2/   (second run)
├── 2024-12-08-3/   (third run)
```

### Standard Output Files

| File | Purpose | Format |
|------|---------|--------|
| `summary.json` | Key metrics | JSON |
| `trades.csv` | All trades | CSV |
| `snapshots.csv` | Daily snapshots | CSV |
| `report.html` | Visual report | HTML |
| `config_used.json` | Parameters for this run | JSON |
| `notes.md` | Any manual notes | Markdown |

---

## 5. Database Naming

### Database File: `trades.db`

Location: `/Strategies/_data/trades.db`

### Table Names: `snake_case`

```sql
GOOD:
  trades
  trade_snapshots
  pre_trade_context
  trade_analytics
  strategy_stats
  uw_alerts

BAD:
  Trades
  TradeSnapshots
  trade-snapshots
```

### Column Names: `snake_case`

```sql
GOOD:
  trade_id
  entry_date
  exit_reason
  pnl_percent
  gex_direction_mult

BAD:
  TradeId
  entryDate
  exit-reason
  pnlPercent
```

---

## 6. Database Schema

### Table: `trades`

Primary trade record. One row per trade.

```sql
CREATE TABLE trades (
    -- Identifiers
    trade_id            TEXT PRIMARY KEY,   -- e.g., "NVDA-2024-12-08-001"
    strategy            TEXT NOT NULL,       -- e.g., "uw-alert-straddle"
    symbol              TEXT NOT NULL,       -- e.g., "NVDA"

    -- Entry
    entry_date          DATE NOT NULL,
    entry_price         REAL NOT NULL,       -- Straddle cost
    entry_dte           INTEGER NOT NULL,    -- Days to expiration at entry
    entry_strike        REAL,                -- ATM strike
    entry_underlying    REAL,                -- Underlying price at entry

    -- Exit
    exit_date           DATE,
    exit_price          REAL,
    exit_dte            INTEGER,
    exit_reason         TEXT,                -- MOMENTUM_EXIT, TIME_STOP, STOP_LOSS, etc.

    -- Results
    pnl_dollars         REAL,
    pnl_percent         REAL,
    days_held           INTEGER,

    -- Entry Conditions (what triggered this trade)
    entry_alert_count   INTEGER,             -- UW alert density
    entry_ivr           REAL,
    entry_vix           REAL,
    entry_net_gex       REAL,
    entry_gex_direction TEXT,                -- "falling" or "rising"

    -- Sizing Factors Used
    conviction_mult     REAL,                -- 1.0, 1.25, or 1.5
    gex_magnitude_mult  REAL,                -- 0.7 to 2.5
    gex_direction_mult  REAL,                -- 0.7 or 1.5
    position_size       REAL,                -- Final $ amount

    -- Metadata
    created_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    notes               TEXT
);
```

### Table: `trade_snapshots`

Daily data while trade is open. One row per trade per day.

```sql
CREATE TABLE trade_snapshots (
    -- Keys
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    trade_id            TEXT NOT NULL,
    snapshot_date       DATE NOT NULL,
    day_number          INTEGER NOT NULL,    -- Day 1, 2, 3... of trade

    -- Position State
    straddle_price      REAL,
    call_price          REAL,
    put_price           REAL,
    position_pnl_pct    REAL,
    pnl_from_peak       REAL,                -- For momentum exit tracking

    -- Greeks
    delta               REAL,
    gamma               REAL,
    theta               REAL,
    vega                REAL,

    -- Underlying
    underlying_price    REAL,
    underlying_change   REAL,                -- Daily % change

    -- Volatility
    iv_call             REAL,
    iv_put              REAL,
    iv_avg              REAL,
    ivr                 REAL,                -- IV Rank (0-100)
    hv_10               REAL,
    hv_20               REAL,
    hv_30               REAL,
    iv_hv_spread        REAL,                -- IV - HV
    vix                 REAL,

    -- Volume & Flow
    call_volume         INTEGER,
    put_volume          INTEGER,
    total_volume        INTEGER,
    put_call_ratio      REAL,
    open_interest       INTEGER,
    oi_change           INTEGER,

    -- GEX
    net_gex             REAL,
    gex_change          REAL,                -- vs previous day
    gex_direction       TEXT,                -- "falling" or "rising"

    -- Technicals
    rsi_14              REAL,
    sma_5               REAL,
    sma_10              REAL,
    sma_20              REAL,
    sma_50              REAL,
    sma_200             REAL,
    atr_14              REAL,

    -- UW Alerts (if tracking)
    alert_count         INTEGER,

    FOREIGN KEY (trade_id) REFERENCES trades(trade_id),
    UNIQUE(trade_id, snapshot_date)
);
```

### Table: `pre_trade_context`

14 trading days before entry. Same structure as snapshots.

```sql
CREATE TABLE pre_trade_context (
    -- Keys
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    trade_id            TEXT NOT NULL,
    context_date        DATE NOT NULL,
    days_before_entry   INTEGER NOT NULL,    -- -14, -13, ... -1

    -- Same fields as trade_snapshots (underlying, IV, GEX, etc.)
    underlying_price    REAL,
    underlying_change   REAL,
    iv_avg              REAL,
    ivr                 REAL,
    hv_20               REAL,
    vix                 REAL,
    net_gex             REAL,
    gex_change          REAL,
    gex_direction       TEXT,
    call_volume         INTEGER,
    put_volume          INTEGER,
    open_interest       INTEGER,
    rsi_14              REAL,
    sma_20              REAL,
    sma_50              REAL,
    alert_count         INTEGER,

    -- Metadata
    FOREIGN KEY (trade_id) REFERENCES trades(trade_id),
    UNIQUE(trade_id, context_date)
);
```

### Table: `trade_analytics`

Computed metrics per trade. One row per trade.

```sql
CREATE TABLE trade_analytics (
    -- Keys
    trade_id            TEXT PRIMARY KEY,

    -- Time Metrics
    dte_at_entry        INTEGER,
    dte_at_exit         INTEGER,
    days_held           INTEGER,

    -- P&L Journey
    max_peak_profit_pct REAL,
    max_peak_profit_day INTEGER,
    max_drawdown_pct    REAL,
    max_drawdown_day    INTEGER,
    profit_capture_rate REAL,                -- exit_pnl / max_peak

    -- Recovery Analysis (thresholds touched)
    touched_minus_5     BOOLEAN,
    touched_minus_10    BOOLEAN,
    touched_minus_15    BOOLEAN,
    touched_plus_10     BOOLEAN,
    touched_plus_20     BOOLEAN,
    touched_plus_25     BOOLEAN,
    touched_plus_30     BOOLEAN,
    touched_plus_50     BOOLEAN,

    -- Days to Threshold
    days_to_plus_10     INTEGER,
    days_to_plus_20     INTEGER,
    days_to_plus_25     INTEGER,

    -- Classification
    winner              BOOLEAN,             -- pnl > 0

    FOREIGN KEY (trade_id) REFERENCES trades(trade_id)
);
```

### Table: `strategy_stats`

Aggregate statistics. One row per strategy per time period.

```sql
CREATE TABLE strategy_stats (
    -- Keys
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    strategy            TEXT NOT NULL,
    period_type         TEXT NOT NULL,       -- "all_time", "ytd", "monthly", "quarterly"
    period_start        DATE,
    period_end          DATE,

    -- Trade Counts
    total_trades        INTEGER,
    winners             INTEGER,
    losers              INTEGER,
    win_rate            REAL,

    -- Returns
    total_return_pct    REAL,
    avg_return_pct      REAL,
    median_return_pct   REAL,
    avg_win_pct         REAL,
    avg_loss_pct        REAL,
    best_trade_pct      REAL,
    worst_trade_pct     REAL,

    -- Risk Metrics
    sharpe_ratio        REAL,
    sortino_ratio       REAL,
    calmar_ratio        REAL,
    max_drawdown_pct    REAL,
    avg_drawdown_pct    REAL,

    -- Time Metrics
    avg_days_held       REAL,
    avg_days_winners    REAL,
    avg_days_losers     REAL,
    avg_days_to_peak    REAL,
    avg_profit_capture  REAL,

    -- Statistical Significance
    t_statistic         REAL,
    p_value             REAL,
    confidence_95_low   REAL,
    confidence_95_high  REAL,

    -- Profit Metrics
    profit_factor       REAL,                -- gross_wins / gross_losses
    expectancy          REAL,                -- avg $ per trade

    -- Metadata
    calculated_at       TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    UNIQUE(strategy, period_type, period_start, period_end)
);
```

---

## 7. Exit Reason Codes

### Pattern: `UPPER_SNAKE_CASE`

| Code | Meaning | Description |
|------|---------|-------------|
| `MOMENTUM_EXIT` | Hit profit target then dropped | +25% peak, -3% daily drop |
| `TIME_STOP` | Time-based exit | Day 30, or DTE < X |
| `STOP_LOSS` | Loss limit hit | e.g., -15% |
| `PROFIT_TARGET` | Fixed profit target | e.g., +50% |
| `GEX_SIGNAL` | GEX-based exit | GEX flipped |
| `MANUAL` | Manual exit | Human decision |
| `EARNINGS` | Earnings approaching | Risk management |
| `EXPIRATION` | Near expiration | DTE too low |

---

## 8. Trade ID Format

### Pattern: `{SYMBOL}-{YYYY-MM-DD}-{NNN}`

```
GOOD:
  NVDA-2024-12-08-001
  TSLA-2024-12-08-002
  AAPL-2024-12-15-001

BAD:
  NVDA_20241208_1
  nvda-2024-12-08
  trade_001
```

### Rules
- Symbol: Uppercase ticker
- Date: Entry date
- NNN: Sequential number for that symbol on that day (usually 001)

---

## 9. Config Schema

### Standard `config.json` Structure

```json
{
  "_meta": {
    "strategy": "uw-alert-straddle",
    "version": "8.2",
    "last_updated": "2024-12-09",
    "description": "UW Alert Straddle with GEX Direction-Aware Sizing"
  },

  "universe": {
    "symbols": ["NVDA", "TSLA", "AAPL", "META", "AMZN", "GOOGL"],
    "min_options_volume": 10000,
    "min_market_cap": 50000000000
  },

  "entry": {
    "alert_density_min": 50,
    "dte_min": 45,
    "dte_max": 60,
    "vix_min": 9,
    "vix_max": 28,
    "earnings_buffer_days": 60,
    "entry_time": "30min_after_open"
  },

  "sizing": {
    "base_position_usd": 20000,
    "conviction_thresholds": {
      "tier1_min_alerts": 50,
      "tier1_mult": 1.0,
      "tier2_min_alerts": 100,
      "tier2_mult": 1.25,
      "tier3_min_alerts": 200,
      "tier3_mult": 1.5
    },
    "gex_magnitude": {
      "min_mult": 0.7,
      "max_mult": 2.5
    },
    "gex_direction": {
      "falling_mult": 1.5,
      "rising_mult": 0.7
    },
    "max_concentration_pct": 5
  },

  "exit": {
    "momentum": {
      "enabled": true,
      "peak_threshold_pct": 25,
      "drop_from_peak_pct": 3
    },
    "time_stop": {
      "enabled": true,
      "max_days": 30
    },
    "stop_loss": {
      "enabled": false,
      "threshold_pct": -15
    },
    "profit_target": {
      "enabled": false,
      "threshold_pct": 50
    }
  },

  "risk": {
    "max_open_positions": 10,
    "max_daily_trades": 5,
    "max_portfolio_risk_pct": 20
  }
}
```

---

## 10. Code Style (For AI Reference)

### Python Style

```python
# Classes: PascalCase
class UWAlertStraddle(QCAlgorithm):
    pass

# Functions/methods: snake_case
def calculate_gex_direction(self, today_gex, yesterday_gex):
    pass

# Variables: snake_case
entry_alert_count = 150
gex_direction_mult = 1.5

# Constants: UPPER_SNAKE_CASE
MAX_POSITION_SIZE = 100000
DEFAULT_DTE_MIN = 45

# Private methods: _leading_underscore
def _validate_entry_conditions(self):
    pass
```

### SQL Style

```sql
-- Tables: snake_case, plural
SELECT * FROM trades;
SELECT * FROM trade_snapshots;

-- Columns: snake_case
SELECT trade_id, entry_date, pnl_percent FROM trades;

-- Joins: explicit
SELECT t.trade_id, ts.snapshot_date
FROM trades t
JOIN trade_snapshots ts ON t.trade_id = ts.trade_id;
```

---

## 11. Git Commit Messages

### Pattern: `{type}: {description}`

```
GOOD:
  feat: add GEX direction sizing to V8.2
  fix: correct profit capture calculation
  docs: update STRATEGY.md with new exit rules
  refactor: simplify position sizing logic
  data: add 2023 backtest results

BAD:
  updated stuff
  fixed bug
  WIP
  changes
```

### Types
| Type | Use For |
|------|---------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Code change without behavior change |
| `data` | Data additions/changes |
| `test` | Test additions |
| `config` | Configuration changes |

---

## 12. Backtest Report Standards

### Required Metrics (Always Include)

| Category | Metrics |
|----------|---------|
| **Capital** | Total Deployed, Avg Daily at Risk, Max Daily at Risk |
| **ROC** | Simple ROC, Annualized ROC (on capital at risk), Avg Trade ROC |
| **Returns** | Net Profit $, Net Profit %, Avg Profit/Trade |
| **Win/Loss** | Win Rate, Avg Winner %, Avg Loser %, Profit Factor |
| **Risk** | Max Drawdown, Sharpe, Sortino |
| **Time** | Avg Days Held (winners vs losers), Days to Target |

### ROC Calculations

```
Simple ROC = Total PnL / Total Capital Deployed

Annualized ROC = (Total PnL / Avg Daily Capital at Risk) × (252 / Trading Days) × 100

Avg Trade ROC = Mean of all individual trade return %
```

### Scaling Table (Always Include)

Show what returns would look like at different position sizes:

```
| Portfolio | Position (%) | Position ($) | Projected PnL | Portfolio ROC |
|-----------|--------------|--------------|---------------|---------------|
| $100K     | 10%          | $10K         | $X            | X%            |
| $250K     | 10%          | $25K         | $X            | X%            |
```

### Monthly Breakdown (Always Include)

```
| Month | Trades | Wins | Losses | Win% | Total PnL | Avg PnL% |
```

### Exit Analysis (Always Include)

Group trades by exit reason and show:
- Count of trades per exit type
- Win rate per exit type
- Avg PnL per exit type
- Avg days held per exit type

---

## Quick Reference Card

```
DIRECTORIES:     lowercase-with-dashes     uw-alert-straddle/
PYTHON FILES:    snake_case.py             trade_analyzer.py
IMPORTANT DOCS:  UPPER_CASE.md             STRATEGY.md
CONFIG:          config.json               (always)
VERSIONS:        v{N}_{YYYY-MM-DD}.py      v8_2024-12-06.py
BACKTESTS:       YYYY-MM-DD/               2024-12-08/
DB TABLES:       snake_case                trade_snapshots
DB COLUMNS:      snake_case                entry_date
EXIT CODES:      UPPER_SNAKE_CASE          MOMENTUM_EXIT
TRADE IDS:       {SYM}-{DATE}-{NNN}        NVDA-2024-12-08-001
CLASSES:         PascalCase                UWAlertStraddle
FUNCTIONS:       snake_case                calculate_gex()
CONSTANTS:       UPPER_SNAKE_CASE          MAX_POSITION_SIZE
```

---

## Changelog

| Date | Change |
|------|--------|
| 2024-12-09 | Initial creation |
