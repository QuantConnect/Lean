# SPY Wheel Strategy - Sunny

## Name

Sunny

## Date Completed

June 4, 2026

## Strategy Modifications

This project modifies the default QuantConnect sample algorithm into a simplified wheel strategy.

Changes made:

1. Renamed the strategy to "SPY Wheel Strategy - Sunny"
2. Increased starting capital from $100,000 to $150,000
3. Added a target 30-day expiration parameter
4. Added wheel strategy phase logic:

   * Phase 1: Cash-Secured Put
   * Phase 2: Assignment
   * Phase 3: Covered Call
5. Added explanatory comments throughout the code

## Biggest Challenge Encountered

The biggest challenge was configuring the LEAN CLI environment and resolving the `pkg_resources` error caused by the installed setuptools version.

## What I Learned

Through this exercise I learned:

* How to use QuantConnect LEAN CLI
* How to run local backtests
* How wheel strategies operate conceptually
* How to debug Python and environment issues
* How to modify and test trading algorithms in QuantConnect

