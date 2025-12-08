# Analytics Directory

> **Shared analysis tools used across all strategies.**

---

## Purpose

This directory contains Python scripts for analyzing trade data. These tools work with the SQLite database in `_data/trades.db`.

---

## Available Tools

| File | Purpose | Usage |
|------|---------|-------|
| `trade_analyzer.py` | Grouped trade view, P&L journey | Analyze individual trades |
| `monthly_report.py` | Monthly breakdown, streaks, drill-downs | Monthly performance review |
| `trajectory_analysis.py` | Recovery probabilities | "If -10%, what's P(+20%)?" |
| `strategy_stats.py` | Statistical significance, Sharpe, etc. | Strategy validation |
| `export_report.py` | Generate PDF/HTML reports | End-of-month reporting |

---

## Usage Pattern

All tools read from `_data/trades.db` and output to console or files.

```bash
# Example (when implemented)
python trade_analyzer.py --trade-id NVDA-2024-12-08-001
python monthly_report.py --strategy uw-alert-straddle --month 2024-11
python strategy_stats.py --strategy uw-alert-straddle
```

---

## Adding New Tools

1. Follow naming convention: `snake_case.py`
2. Read from `_data/trades.db`
3. Document in this README
4. Keep tools focused (one job per script)

---

*Last updated: 2024-12-09*
