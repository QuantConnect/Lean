# SESSION_LOG.md - Running Activity Log

> **Append-only log. Never delete, only add.**

---

## 2024-12-09 - Session: Initial Setup + First Backtest

### Context
- Building professional quant trading system on QuantConnect
- Non-coder owner (Junaid), AI-assisted development
- Goal: Test options strategies, find edge through data exploration

### Goals This Session
1. Set up infrastructure (folders, conventions, database)
2. Create first strategy: Low IVR Straddle
3. Run first backtest
4. Analyze results with proper ROC metrics

### Activity Log

- [Session Start] Continued from previous session (context compacted)
- Reviewed STRATEGY.md for Low IVR Straddle
- Reviewed LowIVRStraddle.py algorithm
- Configured QuantConnect MCP server
- Set organization ID: b69ac21225c198880c7f1a0e8f1d7f97
- Ran first cloud backtest successfully!

### Backtest Results: LowIVRStraddle v1.0

**Summary:**
- Period: Jan 1 - Dec 31, 2024
- Net Profit: +$56,786 (+4.93% portfolio, +5.21% ROC)
- Total Trades: 116 straddles
- Win Rate: 48%

**Key Findings:**
- Profit target exits: 42 trades, +31.5% avg, work great
- Time stop exits: 74 trades, -10.2% avg, 22% win rate (problem!)
- Annualized ROC on capital at risk: 75.98%
- Feb best month (70% win), May worst (32% win)

**Top Winners:**
- SMCI Aug: +57%
- AAPL Jun: +66% in 2 days
- GOOGL Feb: +60%

**Issues Found:**
1. Two separate MarketOrders causing leg fill issues (QC AI fixing)
2. No warmup period - Jan had no trades (QC AI fixing)
3. IVR < 40 threshold too loose - too many trades hitting time stop

### Decisions Made
- ROC metrics now standard in all reports (added to CONVENTIONS.md)
- Position sizing analysis shows 76% annualized ROC on actual capital at risk
- Time stop is the main problem to solve

### Current Blockers
- Waiting for QC AI fixes (combo orders + warmup period)

### Completed This Session
- [x] Warmup fix applied (+29% profit increase)
- [x] Expanded universe to 31 tickers (+$10K from new tickers)
- [x] Identified best new tickers: QCOM, MU, CRM
- [x] Identified worst new tickers: ORCL, INTC, GOOG
- [x] Decision: Fix exit rules FIRST, then expand universe

### Key Insight
**Time stop is the leak:** 79% of time-stopped trades are losers (-9.6% avg)
**Profit targets work:** +33.7% avg on early exits

### Next Session
1. User will provide their exit rules to test
2. Then test proposed variations:
   - Add -15% or -20% stop loss
   - Extend to Day 25
   - Lower profit target to 20%
   - Trailing stop after +15%
3. Find best exit config on 15 tickers
4. Then expand universe with optimized exits

### Git Checkpoints
- `078505cb6` - Memory system + first backtest
- `57df4082d` - Warmup fix applied
- `04e3b8eab` - Expanded universe test

---

## 2024-12-09 - Session: Exit Rule Tests + Lifecycle Tracking

### Backtest Results Summary

| Test | Exit Rules | Net Profit | ROC | Win Rate |
|------|------------|------------|-----|----------|
| Baseline | +25% OR Day 20 | $73,289 | 78% | 47% |
| Test 1 | Momentum (4% drop) + Day 20 | $65,659 | - | 41% |
| **Test 2** | **Momentum + Day 30** | **$134,241** | **126%** | **54%** |
| Test 3 | Hold to Expiration | $163,206 | 129% | 40% |

### Key Findings
- **Test 2 (Momentum + Day 30) is the winner** - best risk-adjusted returns
- Momentum exit captures 82% of potential with 58% less drawdown
- Hold to expiration makes MORE money but 2.4x worse drawdown

### Capture Rate Analysis
- Test 2 captured $134K of $164K potential = 82%
- Left on table: $29,674 (18%)
- But: $39.5K per 1% drawdown (vs $20.2K for hold-to-exp)

### Lifecycle Tracking Implementation (In Progress)
Added to main.py:
- RSI(14) and ADX(14) indicators on underlying
- Daily snapshot logging with: P&L, IV ratio, Greeks, signals
- Peak tracking for true capture rate calculation
- EXIT_SUMMARY with capture rate and days from peak

### Files Modified
- `/Users/junaidhassan/Lean/LowIVRStraddle/main.py` - Full lifecycle tracking
- `/Users/junaidhassan/Lean/Strategies/_templates/BACKTEST_REPORT.template.md` - Report template

### Next Steps
1. Update OnEndOfAlgorithm with runtime statistics
2. Run lifecycle backtest with 30 tickers
3. Create CSV export script for log parsing
4. Analyze true capture rates and signal effectiveness

---

## Template for New Sessions

```
## YYYY-MM-DD HH:MM - Session: [Brief Description]

### Context
- What project/phase
- Key constraints

### Goals This Session
1. Goal 1
2. Goal 2

### Activity Log
- [HH:MM] Activity
- [HH:MM] Activity

### Decisions Made
- Decision 1
- Decision 2

### Blockers
- Blocker 1

### Next Steps
- Step 1
- Step 2
```

