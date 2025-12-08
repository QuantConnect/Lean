# Strategies Directory

> **All trading strategies live here, organized by lifecycle stage.**

---

## Directory Structure

```
Strategies/
├── _templates/      # Reusable templates (STRATEGY.md, config.json)
├── _analytics/      # Shared analysis tools
├── _data/           # Trade database and shared data
│
├── active/          # Strategies in development/testing
├── paper/           # Paper trading (validated, not live yet)
├── live/            # Deployed with real money
└── archive/         # Retired strategies (keep for reference)
```

---

## Strategy Lifecycle

```
IDEA → active/ → paper/ → live/ → archive/
         ↑                           │
         └───────────────────────────┘
              (if live fails)
```

| Stage | Folder | What Happens |
|-------|--------|--------------|
| **Development** | `active/` | Building, testing, iterating |
| **Paper Trading** | `paper/` | Real-time validation without money |
| **Live** | `live/` | Real money at risk |
| **Retired** | `archive/` | No longer trading (keep for learning) |

---

## Creating a New Strategy

1. Copy template: `cp -r _templates/strategy-template active/my-new-strategy`
2. Rename to follow convention: `lowercase-with-dashes`
3. Fill out `STRATEGY.md` with hypothesis, rules, parameters
4. Develop algorithm in `algorithm.py`
5. Run backtests, store results in `backtests/YYYY-MM-DD/`

---

## Standard Strategy Folder Contents

```
my-strategy/
├── STRATEGY.md      # Specification (required)
├── algorithm.py     # Code (required)
├── config.json      # Parameters (required)
├── CHANGELOG.md     # Version history (required)
├── RESEARCH.md      # Research notes (optional)
├── versions/        # Previous iterations
│   ├── v1_2024-12-01.py
│   └── v2_2024-12-08.py
└── backtests/       # Results by date
    └── 2024-12-08/
        ├── summary.json
        ├── trades.csv
        └── report.html
```

---

## Naming Conventions

See `/CONVENTIONS.md` for complete rules.

Quick reference:
- Folder: `lowercase-with-dashes`
- Files: `snake_case.py`
- Versions: `v{N}_{YYYY-MM-DD}.py`

---

## Current Active Strategies

| Strategy | Status | Description |
|----------|--------|-------------|
| `uw-alert-straddle` | Research | UW alerts + straddles (hypothesis testing) |

---

*Last updated: 2024-12-09*
