# Strategy: Low IVR Straddle

> Buy straddles when IV is cheap, exit on profit or time.

---

## Status

| Field | Value |
|-------|-------|
| **Status** | Testing |
| **Version** | v1.0 |
| **Created** | 2024-12-09 |
| **Algorithm File** | `/Algorithm.Python/LowIVRStraddle.py` |

---

## Hypothesis

Buy straddles when IV is LOW (cheap). The edge comes from:
1. Cheaper entry = less to lose
2. IV expansion potential
3. Time-based exit prevents theta decay destruction

---

## Universe

```
NVDA, TSLA, META, AMD, AAPL, SMCI, MSFT, AMZN, NFLX, COIN, GOOGL, AVGO, PLTR, LULU, ADBE
```

15 high-liquidity stocks with active options markets.

---

## Entry Rules

| # | Condition | Value | Rationale |
|---|-----------|-------|-----------|
| 1 | DTE | 45-70 days | Enough time for move, not too much theta |
| 2 | IVR | < 40 | Buy when IV is LOW (cheap straddles) |
| 3 | Earnings | > 21 days away | Never hold through earnings (IV crush) |
| 4 | Bid-Ask Spread | < 4% | Liquidity filter |
| 5 | Ticker Cooldown | 5 days since last entry | Avoid overconcentration |

---

## Exit Rules

| Priority | Condition | Reason Code |
|----------|-----------|-------------|
| 1 | +25% profit | PROFIT_TARGET |
| 2 | Day 20 | TIME_STOP |

**Whichever comes first wins.**

---

## Position Sizing

| Parameter | Value |
|-----------|-------|
| Size per trade | $10,000 |
| Max same ticker | 1 position (5-day cooldown) |
| Starting capital | $1,000,000 |

---

## Parameters

```json
{
  "universe": ["NVDA", "TSLA", "META", "AMD", "AAPL", "SMCI", "MSFT", "AMZN", "NFLX", "COIN", "GOOGL", "AVGO", "PLTR", "LULU", "ADBE"],
  "position_size": 10000,
  "dte_min": 45,
  "dte_max": 70,
  "ivr_max": 40,
  "max_spread_pct": 0.04,
  "earnings_buffer_days": 21,
  "profit_target_pct": 0.25,
  "max_hold_days": 20,
  "ticker_cooldown_days": 5
}
```

---

## Known Limitations

- **Earnings check is placeholder** - needs UW or external data integration
- **IVR needs warmup** - first 20 days use default IVR=50

---

## Changelog

| Date | Version | Change |
|------|---------|--------|
| 2024-12-09 | v1.0 | Initial version - first backtest |
