# CLAUDE.md - AI Assistant Context & Memory

> **This file is read by Claude Code at the start of every session.**
> It provides persistent memory and context across conversations.
> UPDATE THIS FILE as the project evolves.

---

## Project Owner

**Junaid Hassan**
- Background: Engineering + Business degrees, $40M ecommerce business over 9 years
- Trading: Mentored by hedge fund trader, achieved 80% and 210% ROC
- Style: "Relaxed savage" - decisive, aims for best-in-class, will cut what doesn't work
- Constraint: Non-coder, relies on AI for implementation
- Workflow preference: Conversation-driven development

---

## Project Overview

**Core Hypothesis:** "Follow the Money"
> Does unusual options activity (regardless of reason) give us an edge?
> If we put a straddle on every alert and explore them, what would we find?

**Goal:** Build a professional-grade quantitative trading system to:
1. Track and analyze trades with institutional-grade metrics
2. Discover the REAL edge through data exploration (not assumptions)
3. Use UW alerts as one signal factor among many (not sole entry trigger)
4. Eventually deploy live with proper risk management

**Current Phase:** Phase 0 - Foundation
- Building fail-proof infrastructure FIRST
- Establishing naming conventions, folder structure, documentation
- Creating systems that support discovery, not locked-in strategies

---

## Critical Learnings from BigQuery Research

> **These learnings shape how we're building the system.**

### What Worked (Keep)
- GEX direction matters: falling GEX = larger moves
- Conviction sizing based on signal strength
- Per-ticker normalization (not global averages)

### What Didn't Work / Limitations
| Issue | Reality |
|-------|---------|
| **EOD Exit Rules** | +25% peak → -3% drop works on daily snapshots, but intraday price can spike and drop within hours. Not tradeable. |
| **Universe Concentration** | Using UW alerts to build universe = same ~10 stocks repeatedly. Concentration risk, not diversification. |
| **V8.2 Returns** | +585% likely overfitted to EOD methodology. Real edge is probably smaller and different. |

### Revised Understanding
- **UW alerts:** Better as a **scoring factor** than sole entry signal
- **The real edge:** Still to be discovered through proper testing
- **Infrastructure first:** Build systems to explore, not to execute one strategy

---

## Research Summary: What Junaid Has Discovered

### The Edge (Validated)

**V8.2 Backtest Results (2024):**
- $1M → $6.85M (+585% annual return)
- 777 trades
- Won 8/12 months

**Out-of-Sample (2023):**
- Edge validated: +44.8% to +142% depending on density threshold
- Won 10/12 months
- Key finding: Density threshold is regime-dependent

### Current Best Parameters (V8.2 + GEX Direction-Aware)

| Parameter | Value | Notes |
|-----------|-------|-------|
| **DTE** | 45-60 days | Optimal theta/gamma balance |
| **VIX Range** | 9-28 | Avoid extremes |
| **Earnings** | None within 60 days | Avoid binary events |
| **Signal Density** | ≥50 alerts/ticker/day (2024) | Regime-dependent |
| **Deduplication** | Ticker-day level | Multiple alerts = one trade decision |
| **Max Concentration** | 5% per ticker | Risk management |
| **Exit: Profit** | +25% from peak, then -3% daily drop | Momentum exit |
| **Exit: Time** | Day 30 time stop | Avoid theta decay |

### Position Sizing Formula (V8.2)

```python
# Base position
base_size = $20,000

# Conviction multiplier (based on alert count)
conviction_mult = 1.0   # if alerts 50-99
conviction_mult = 1.25  # if alerts 100-199
conviction_mult = 1.5   # if alerts 200+

# GEX Magnitude multiplier (per-ticker normalized)
gex_magnitude_mult = max(0.7, min(2.5, 1 + abs(net_gex) / ticker_avg_abs_gex))

# GEX Direction multiplier (KEY DISCOVERY)
gex_change = today_net_gex - yesterday_net_gex
gex_direction_mult = 1.5 if gex_change < 0 else 0.7
# Falling GEX = amplified moves = size UP
# Rising GEX = dampened moves = size DOWN

# Final position size
position_size = base_size × conviction_mult × gex_magnitude_mult × gex_direction_mult
```

### Why GEX Direction Matters

| GEX Direction | % of Trades | Avg Return | Strategy Response |
|---------------|-------------|------------|-------------------|
| **Falling** | 51% | +22.6% | Size UP 1.5x |
| **Rising** | 49% | +16.8% | Size DOWN 0.7x |

**Theory:** Falling GEX = market makers reducing gamma hedging = amplified price moves = straddles benefit more.

### Evolution of Strategy (For Context)

| Version | Annual Return | Key Change |
|---------|---------------|------------|
| Baseline ($20K flat) | +235% | Simple approach |
| +Conviction Sizing | +296% | Alert count sizing |
| +Conviction +GEX Magnitude | +524% | Per-ticker GEX normalization |
| **+GEX Direction (V8.2)** | **+585%** | Direction-aware sizing |

---

## Data Assets Available

| Asset | Details | Status |
|-------|---------|--------|
| **Unusual Whales** | 2 years alerts + API | Have it |
| **ORATS EOD** | 2007-2025 (18 years options data) | Have it |
| **ORATS API** | Real-time access | Have it |
| **QuantConnect/Lean** | Local at `/Users/junaidhassan/Lean` | Setting up |
| **Google Cloud/BigQuery** | Heavy compute, current research | Have it |
| **Tastytrade** | Execution | Have it |
| **TradingView Pro** | Charting/analysis | Have it |

---

## Working Agreement with AI

### Junaid's Role
- Describe strategy intent in plain English
- Validate that implementation matches intent
- Review results and reports
- Make trading decisions
- NEVER debug code or deal with technical errors

### AI's Role
- Translate intent to code
- Handle ALL technical implementation
- Maintain documentation and memory files
- Explain what was built in plain terms
- Surface issues as decisions, not technical problems

### Communication Style
- Be direct, no fluff
- Present options with clear trade-offs
- Use tables for comparisons
- Show results, not just descriptions
- When something is wrong, say so

---

## File Structure

```
/Users/junaidhassan/Lean/
├── CLAUDE.md                    # THIS FILE - AI memory
├── CONVENTIONS.md               # Naming rules and standards
├── WORKFLOW.md                  # How Junaid + AI work together
├── Algorithm.Python/            # QuantConnect's folder (deployed algos only)
│
└── Strategies/                  # Main workspace
    ├── README.md                # How strategies are organized
    │
    ├── _templates/              # Reusable templates
    │   ├── STRATEGY.template.md
    │   └── config.template.json
    │
    ├── _analytics/              # Shared analysis tools
    │   ├── README.md
    │   ├── trade_analyzer.py
    │   ├── monthly_report.py
    │   └── trajectory_analysis.py
    │
    ├── _data/                   # Shared data and database
    │   ├── README.md
    │   └── trades.db            # SQLite trade database
    │
    ├── active/                  # Strategies in development
    │   └── uw-alert-straddle/   # Main strategy
    │       ├── STRATEGY.md      # Full specification (V8.2)
    │       ├── algorithm.py     # QuantConnect implementation
    │       ├── config.json      # Parameters
    │       ├── CHANGELOG.md     # Version history
    │       ├── RESEARCH.md      # Link to BigQuery research
    │       ├── versions/        # Previous iterations
    │       └── backtests/       # Results by date
    │
    ├── live/                    # Deployed to real money
    ├── paper/                   # Paper trading
    └── archive/                 # Retired strategies
```

---

## Naming Conventions

**See `CONVENTIONS.md` for complete rules.**

Quick reference:
- Strategy folders: `lowercase-with-dashes` (e.g., `uw-alert-straddle`)
- Python files: `snake_case.py` (e.g., `trade_analyzer.py`)
- Config files: `config.json` (always this name)
- Versions: `v{N}_{YYYY-MM-DD}.py` (e.g., `v8_2024-12-06.py`)
- Backtests: `YYYY-MM-DD/` folders (e.g., `2024-12-08/`)
- Database tables: `snake_case` (e.g., `trade_snapshots`)
- Exit reason codes: `UPPER_SNAKE_CASE` (e.g., `MOMENTUM_EXIT`, `TIME_STOP`)

---

## Current Strategy: UW Alert Straddle

**Folder:** `/Strategies/active/uw-alert-straddle/`
**Version:** V8.2 (GEX Direction-Aware)
**Research Location:** BigQuery (to be documented in RESEARCH.md)

### Entry Rules (V8.2)
| # | Condition | Value | Notes |
|---|-----------|-------|-------|
| 1 | UW Alert Density | ≥50 alerts/ticker/day | Regime-dependent |
| 2 | DTE | 45-60 days | Target range |
| 3 | VIX | 9-28 | Avoid extremes |
| 4 | Earnings | None in 60 days | Avoid binary |
| 5 | Dedup | Ticker-day level | One decision per ticker/day |

### Position Sizing (V8.2)
| Factor | Range | Logic |
|--------|-------|-------|
| Base | $20,000 | Starting point |
| Conviction | 1.0x / 1.25x / 1.5x | Based on alert count |
| GEX Magnitude | 0.7x - 2.5x | Per-ticker normalized |
| GEX Direction | 0.7x / 1.5x | Falling = up, Rising = down |
| Max Concentration | 5% | Per ticker limit |

### Exit Rules (V8.2)
| # | Type | Condition | Reason Code |
|---|------|-----------|-------------|
| 1 | Momentum | +25% peak, then -3% daily drop | MOMENTUM_EXIT |
| 2 | Time | Day 30 | TIME_STOP |
| 3 | Stop (TBD) | To be determined | STOP_LOSS |

---

## Database Schema

**Location:** `/Strategies/_data/trades.db` (SQLite)

### Tables
1. `trades` - One row per trade
2. `trade_snapshots` - Daily data while trade is open
3. `pre_trade_context` - 14 trading days before each entry
4. `trade_analytics` - Computed metrics per trade
5. `strategy_stats` - Aggregate statistics
6. `uw_alerts` - Raw Unusual Whales data (if imported)

See `CONVENTIONS.md` for full schema.

---

## Key Metrics to Track

### Per Trade
- Entry/exit dates, prices, DTE
- P&L ($ and %)
- Exit reason code
- Days held
- Max peak profit, max drawdown
- Profit capture rate
- Alert count at entry
- GEX magnitude and direction at entry
- Conviction/sizing multipliers used

### Per Day (while in trade)
- Position P&L, peak from entry
- Underlying price, daily change
- Greeks (delta, gamma, theta, vega)
- IV, IVR, HV
- Call/put volume, OI
- Net GEX (and direction)

### Pre-Trade (14 trading days before)
- Daily metrics leading to entry
- Used to identify setup patterns, refine entry timing

### Strategy Level
- Sharpe, Sortino, Calmar ratios
- Win rate, profit factor, expectancy
- Statistical significance (t-stat, p-value, deflated Sharpe)
- Monthly breakdown with streaks
- Regime analysis (by VIX level, by year, by density)

---

## Migration Path: BigQuery → QuantConnect

### Current State (BigQuery)
- Historical backtests complete (V8.2)
- SQL-based analysis
- No execution capability
- 500M+ rows of data

### Target State (QuantConnect/Lean)
- Replicatable backtests with realistic fills
- Options execution modeling
- Trade-by-trade logging
- Path to live trading

### Migration Steps
1. Document V8.2 logic completely in STRATEGY.md
2. Build QuantConnect version that matches BigQuery results
3. Validate: QuantConnect results should approximate BigQuery
4. Add features BigQuery couldn't do (realistic fills, Greeks tracking)

---

## Commands & Workflows

### Run a Backtest
```bash
cd /Users/junaidhassan/Lean/Launcher
dotnet QuantConnect.Lean.Launcher.dll
```

### Configuration
Edit `/Users/junaidhassan/Lean/Launcher/config.json`:
- `algorithm-type-name`: Class name of strategy
- `algorithm-location`: Path to .py file

---

## Session Checklist for AI

When starting a new session, AI should:
1. Read this CLAUDE.md file
2. Check current phase and outstanding work
3. Ask what Junaid wants to work on
4. Reference STRATEGY.md for any strategy work
5. Update this file if significant changes are made
6. Keep todo list updated

---

## Changelog

| Date | Change |
|------|--------|
| 2024-12-09 | Initial creation - Phase 0 foundation |
| 2024-12-09 | Updated with V8.2 research context and correct hypothesis |

---

## Notes & Decisions

*Add important decisions and context here as the project evolves.*

### Foundation Decisions
- **2024-12-09:** SQLite chosen for trade database (simple, portable, queryable)
- **2024-12-09:** Strategy folders use `lowercase-with-dashes` convention
- **2024-12-09:** Full folder structure created with templates, analytics, data directories

### Research Learnings (BigQuery Phase)
- **2024-12-09:** Core hypothesis is "Follow the Money" - UW alerts as signal
- **2024-12-09:** V8.2 achieved +585% on EOD data but has limitations
- **2024-12-09:** Key insight: GEX direction matters (falling = better for straddles)
- **2024-12-09:** Key insight: Density threshold is regime-dependent (50 for 2024, 15-20 for 2023)

### Critical Realizations (Start Fresh)
- **2024-12-09:** EOD exit rules (25% peak, -3% drop) don't work intraday - need new approach
- **2024-12-09:** UW alerts concentrated trades in ~10 stocks - universe problem
- **2024-12-09:** V8.2 results likely overfitted to EOD methodology
- **2024-12-09:** UW alerts better as SCORING FACTOR, not sole entry signal
- **2024-12-09:** Real edge still to be discovered - infrastructure supports exploration

### Infrastructure Built (Phase 0)
- **2024-12-09:** CLAUDE.md - AI memory and context
- **2024-12-09:** CONVENTIONS.md - All naming rules and DB schema
- **2024-12-09:** WORKFLOW.md - How Junaid + AI collaborate
- **2024-12-09:** Folder structure with templates, READMEs
- **2024-12-09:** SQLite database created with full schema

### Current Strategy: Low IVR Straddle (v1)
- **2024-12-09:** Created LowIVRStraddle.py with Junaid's rules
- **Location:** `/Algorithm.Python/LowIVRStraddle.py`
- **Spec:** `/Strategies/active/low-ivr-straddle/STRATEGY.md`
- **Rules:** DTE 45-70, IVR<40, +25% profit or Day 20 exit, $10K/trade, 5-day cooldown
- **Universe:** NVDA, TSLA, META, AMD, AAPL, SMCI, MSFT, AMZN, NFLX, COIN, GOOGL, AVGO, PLTR, LULU, ADBE
- **Status:** Algorithm works, but NO LOCAL OPTIONS DATA

### RESOLVED: Options Data
- **2024-12-09:** Configured QC Cloud with org ID b69ac21225c198880c7f1a0e8f1d7f97
- **2024-12-09:** Successfully ran cloud backtest with full options data

### First Backtest Results: Low IVR Straddle v1.0
- **2024-12-09:** Cloud backtest completed
- **Period:** Jan 1 - Dec 31, 2024
- **Net Profit:** +$56,786 (+4.93% portfolio)
- **ROC on capital at risk:** 75.98% annualized
- **Total Trades:** 116 straddles
- **Win Rate:** 48%
- **Avg Winner:** +28% | **Avg Loser:** -16.7%

**Key Finding:** Profit target exits work great (+31.5% avg), time stops don't (22% win rate, -10.2% avg)

### Issues to Fix
- **2024-12-09:** QC AI fixing: combo orders (both legs together)
- **2024-12-09:** QC AI fixing: warmup period (no trades in Jan due to 20-day IV history requirement)
- **2024-12-09:** Consider: IVR < 30, stop loss at -15%, longer hold period

### Memory System Added
- **2024-12-09:** Built foolproof memory system with 5 layers
- **Files:** MEMORY_SYSTEM.md, SESSION_LOG.md, checkpoint.sh
- **Layers:** CLAUDE.md → SESSION_LOG → Episodic Memory → Git checkpoints

### Backtest Results Summary (2024-12-09)

| Test | Tickers | Net Profit | Annualized ROC | Win Rate |
|------|---------|------------|----------------|----------|
| No warmup | 15 | $56,786 | 76% | 48% |
| With warmup | 15 | $73,289 | 78% | 47% |
| Expanded | 31 | $83,147 | 51% | 45% |

**Key Finding:** Time stop is the main leak
- Profit target exits: +33.7% avg (working great)
- Time stop exits: -9.6% avg, 21% win rate (PROBLEM)

**Best new tickers:** QCOM (+$16K), MU (+$8K), CRM (+$4K)
**Worst new tickers:** GOOG (-$6K), CRWD (-$5K), ORCL (-$3K, 0% win)

### NEXT SESSION: Exit Rule Optimization

**Decision:** Fix exit rules FIRST on original 15 tickers, then expand universe.

**Proposed tests:**
1. Add -15% stop loss
2. Add -20% stop loss
3. Extend time stop to Day 25
4. Lower profit target to 20%
5. Trailing stop after +15%

**User's Exit Rules (Momentum Exit):**
```
PROFIT CAPTURE:
1. Check daily at 3:45 PM ET (15 min before close)
2. Wait for profit > 25% ("profit protection mode")
3. Track peak profit from that point
4. If drop from peak >= 4% → EXIT at 3:45 PM
5. If drop < 4% → HOLD

TIME STOP (fallback):
- If profit capture never triggers, exit at Day X

Example:
- Day 3: +30% (peak=30%) → Hold
- Day 4: +28% (drop=2%) → Hold
- Day 5: +25% (drop=5%) → EXIT
```

**TEST PLAN (isolate variables):**
- Test 1: New momentum exit + Day 20 time stop (baseline comparison)
- Test 2: New momentum exit + Day 30 time stop (extended hold)

### Current Algorithm Location
- **Cloud:** LowIVRStraddle project (31 tickers)
- **Local:** `/Users/junaidhassan/Lean/LowIVRStraddle/main.py`
- **Backup:** `/Users/junaidhassan/Lean/Algorithm.Python/LowIVRStraddle.py`

### QC Configuration
- **Org ID:** b69ac21225c198880c7f1a0e8f1d7f97
- **User ID:** 444200
- **MCP Config:** `/Users/junaidhassan/Lean/quantconnect_mcp_config.json`
