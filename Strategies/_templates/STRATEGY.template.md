# Strategy: [NAME]

> **One-line description of the strategy.**

---

## Status

| Field | Value |
|-------|-------|
| **Status** | `Research` / `Testing` / `Paper` / `Live` / `Archived` |
| **Version** | v1.0 |
| **Created** | YYYY-MM-DD |
| **Last Updated** | YYYY-MM-DD |

---

## Hypothesis

> What edge are we trying to exploit? Why should this work?

[Write your hypothesis in plain English. Be specific about WHY you believe this has edge.]

**Example:**
> Unusual options activity indicates informed money positioning. By following this flow with ATM straddles, we capture the subsequent move regardless of direction.

---

## Universe

### Assets Traded
- [List tickers or describe selection criteria]

### Selection Criteria
| Criterion | Value | Reason |
|-----------|-------|--------|
| Min options volume | 10,000 | Liquidity |
| Min market cap | $50B | Avoid manipulation |
| [Add more] | | |

---

## Entry Rules

> All conditions must be TRUE to enter.

| # | Condition | Threshold | Rationale |
|---|-----------|-----------|-----------|
| 1 | [Condition] | [Value] | [Why this matters] |
| 2 | [Condition] | [Value] | [Why this matters] |
| 3 | [Condition] | [Value] | [Why this matters] |

### Entry Timing
- When during the day do we enter?
- Any waiting periods?

---

## Position Structure

| Field | Value |
|-------|-------|
| **Instrument** | ATM Straddle / Strangle / etc. |
| **DTE Target** | XX days |
| **Strike Selection** | ATM / OTM by X% |

---

## Position Sizing

### Base Size
- Starting position size: $XX,XXX

### Sizing Factors
| Factor | Range | Logic |
|--------|-------|-------|
| [Factor 1] | 0.5x - 2.0x | [When to size up/down] |
| [Factor 2] | 0.5x - 2.0x | [When to size up/down] |

### Formula
```
position_size = base_size × factor1 × factor2 × ...
```

### Risk Limits
| Limit | Value |
|-------|-------|
| Max per position | X% of portfolio |
| Max open positions | N |
| Max daily exposure | X% |

---

## Exit Rules

> Exit when ANY of these conditions is TRUE.

| # | Type | Condition | Action | Reason Code |
|---|------|-----------|--------|-------------|
| 1 | Profit | [Condition] | Close | `PROFIT_TARGET` |
| 2 | Loss | [Condition] | Close | `STOP_LOSS` |
| 3 | Time | [Condition] | Close | `TIME_STOP` |
| 4 | Signal | [Condition] | Close | `SIGNAL_EXIT` |

### Exit Priority
If multiple exit conditions are true, which takes precedence?

---

## Parameters

```json
{
  "entry": {
    "param1": "value",
    "param2": "value"
  },
  "sizing": {
    "base_usd": 20000,
    "max_concentration_pct": 5
  },
  "exit": {
    "profit_target_pct": 25,
    "stop_loss_pct": -15,
    "max_days": 30
  }
}
```

See `config.json` for full parameter file.

---

## Data Requirements

### Required Data
| Data | Source | Frequency |
|------|--------|-----------|
| Options prices | QuantConnect | Minute |
| Greeks | QuantConnect | Minute |
| [Custom data] | [Source] | [Freq] |

### External Data
| Data | Source | How Used |
|------|--------|----------|
| UW Alerts | Unusual Whales API | Entry signal |
| GEX | Spotgamma / ORATS | Sizing factor |

---

## Backtest Results

### Summary
| Metric | Value |
|--------|-------|
| Period | YYYY-MM-DD to YYYY-MM-DD |
| Total Trades | N |
| Win Rate | XX% |
| Total Return | XX% |
| Sharpe Ratio | X.XX |
| Max Drawdown | XX% |
| Profit Factor | X.XX |

### Monthly Performance
| Month | Trades | Win% | Return | Max DD |
|-------|--------|------|--------|--------|
| [Month] | N | XX% | XX% | XX% |

### Statistical Significance
| Metric | Value | Interpretation |
|--------|-------|----------------|
| t-statistic | X.XX | |
| p-value | X.XXX | < 0.05 = significant |
| 95% CI | [X%, Y%] | |

---

## Known Limitations

- [Limitation 1]
- [Limitation 2]
- [Conditions where strategy fails]

---

## Research Notes

### What We Learned
- [Key insight 1]
- [Key insight 2]

### Failed Variations
| Variation | Why It Failed |
|-----------|---------------|
| [Tried X] | [Result] |

### Future Ideas
- [ ] Try [variation]
- [ ] Test with [different parameter]

---

## Changelog

| Date | Version | Change |
|------|---------|--------|
| YYYY-MM-DD | v1.0 | Initial version |

---

## Files

| File | Purpose |
|------|---------|
| `STRATEGY.md` | This file |
| `algorithm.py` | Implementation |
| `config.json` | Parameters |
| `CHANGELOG.md` | Detailed version history |
