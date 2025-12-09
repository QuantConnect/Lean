# Backtest Report Template

> **Use this template for EVERY backtest analysis. No shortcuts.**

---

## 1. Test Summary
- **Test Name:** [Name]
- **Backtest ID:** [ID]
- **Period:** [Start] to [End]
- **Universe:** [X tickers]
- **Exit Rules:** [Description]

---

## 2. Capital Metrics (THE MAIN METRICS)

| Metric | Value |
|--------|-------|
| **Total Capital Deployed** | $X |
| **Net Profit** | $X |
| **Simple ROC** | X% |
| **Avg Daily Capital at Risk** | $X |
| **Trading Days** | X |
| **ANNUALIZED ROC** | **X%** |

---

## 3. Trade Statistics

| Metric | Value |
|--------|-------|
| Total Trades | X |
| Winners | X (X%) |
| Losers | X (X%) |
| **Avg Winner** | +X% (X days avg) |
| **Avg Loser** | -X% (X days avg) |
| Profit Factor | X |
| Expectancy | $X per trade |

---

## 4. Exit Analysis (CRITICAL)

| Exit Type | Count | Avg Return | Win Rate |
|-----------|-------|------------|----------|
| Momentum Exit | X | +X% | X% |
| Time Stop | X | -X% | X% |
| Expiration | X | X% | X% |

**Interpretation:** [What does this tell us?]

---

## 5. Monthly Breakdown

| Month | Trades | Wins | Win% | PnL $ |
|-------|--------|------|------|-------|
| 2024-01 | X | X | X% | $X |
| ... | | | | |

**Best Month:** [Month] - [Why?]
**Worst Month:** [Month] - [Why?]

---

## 6. Per-Ticker Breakdown

| Ticker | Trades | Wins | Win% | Avg Days | PnL $ |
|--------|--------|------|------|----------|-------|
| XXX | X | X | X% | X | $X |
| ... | | | | | |

**Best Performers:** [Top 3 with brief reason]
**Worst Performers:** [Bottom 3 - candidates for removal?]

---

## 7. Top 10 Winners

| Symbol | Entry | Exit | Days | PnL % | PnL $ |
|--------|-------|------|------|-------|-------|
| XXX | YYYY-MM-DD | YYYY-MM-DD | X | +X% | $X |

---

## 8. Top 10 Losers

| Symbol | Entry | Exit | Days | PnL % | PnL $ |
|--------|-------|------|------|-------|-------|
| XXX | YYYY-MM-DD | YYYY-MM-DD | X | -X% | -$X |

**Pattern in losers?** [Analysis]

---

## 9. Risk Metrics

| Metric | Value |
|--------|-------|
| Max Drawdown | X% |
| Max Single Trade Loss | -$X (X%) |
| Consecutive Losers (max) | X |
| Sharpe Ratio | X |
| Sortino Ratio | X |

---

## 10. Comparison to Previous Tests

| Metric | Baseline | This Test | Change |
|--------|----------|-----------|--------|
| Net Profit | $X | $X | +/-$X |
| Annualized ROC | X% | X% | +/-X% |
| Win Rate | X% | X% | +/-X% |
| Drawdown | X% | X% | +/-X% |

---

## 11. Key Insights

1. **What worked:** [Bullet points]
2. **What didn't work:** [Bullet points]
3. **Surprising findings:** [Bullet points]

---

## 12. Recommendations

- [ ] [Actionable next step 1]
- [ ] [Actionable next step 2]
- [ ] [Actionable next step 3]

---

*Report generated: [Timestamp]*
