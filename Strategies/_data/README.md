# Data Directory

> **Shared database and data files for all strategies.**

---

## Contents

| File | Purpose |
|------|---------|
| `trades.db` | SQLite database with all trade data |

---

## Database: `trades.db`

Single SQLite file containing all trade tracking data.

### Tables

| Table | Purpose | Rows Per Trade |
|-------|---------|----------------|
| `trades` | Main trade record (entry, exit, P&L) | 1 |
| `trade_snapshots` | Daily data while trade is open | 1 per day held |
| `pre_trade_context` | 14 days before entry | 14 |
| `trade_analytics` | Computed metrics (peak, drawdown, etc.) | 1 |
| `strategy_stats` | Aggregate statistics | 1 per period |

### Schema

Full schema is documented in `/CONVENTIONS.md` (Section 6).

### Querying

```bash
# Open database
sqlite3 trades.db

# Example queries
SELECT * FROM trades WHERE strategy = 'uw-alert-straddle';
SELECT symbol, COUNT(*) FROM trades GROUP BY symbol;
SELECT * FROM strategy_stats WHERE period_type = 'monthly';
```

---

## Backup

The database should be backed up regularly:

```bash
cp trades.db trades_backup_$(date +%Y%m%d).db
```

---

## External Data

Large external datasets (ORATS, UW) should NOT be stored here.
- ORATS files: Store separately, reference via config
- UW alerts: Store separately or in BigQuery

---

*Last updated: 2024-12-09*
