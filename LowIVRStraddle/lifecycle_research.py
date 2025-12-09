# QuantConnect Research Notebook - Lifecycle Analysis
# ====================================================
# This notebook reconstructs daily P&L trajectories for each trade
# and calculates TRUE capture rate (exit vs peak).
#
# Run this in QuantConnect's Research environment.

# %% [markdown]
# # Trade Lifecycle Analysis
# Reconstruct daily P&L for each trade to find peak profit and capture rate.

# %%
from AlgorithmImports import *
import pandas as pd
import numpy as np
from datetime import datetime, timedelta

# Initialize QuantBook for research
qb = QuantBook()

# %% [markdown]
# ## 1. Define Trades from Backtest
# These are the entry/exit dates from our lifecycle backtest.

# %%
# Sample trades - in practice, load these from backtest orders
# Format: (symbol, entry_date, exit_date, strike, entry_cost)
trades = [
    # Add your trades here from the lifecycle_trades.csv
    # Example format:
    # ("NVDA", "2024-01-15", "2024-02-10", 500, 5000),
]

# Or load from CSV
import requests
# If you uploaded lifecycle_trades.csv to ObjectStore, load it here

# %% [markdown]
# ## 2. Function to Get Daily Option Prices

# %%
def get_trade_trajectory(qb, symbol, entry_date, exit_date, strike, entry_cost):
    """
    Reconstruct daily P&L trajectory for a straddle trade.
    Returns DataFrame with daily P&L and metrics.
    """
    # Add the underlying
    equity = qb.AddEquity(symbol, Resolution.Daily)
    option = qb.AddOption(symbol, Resolution.Daily)

    start = datetime.strptime(entry_date, "%Y-%m-%d")
    end = datetime.strptime(exit_date, "%Y-%m-%d")

    # Get option chain history
    option_history = qb.OptionChainProvider.GetOptionContractList(equity.Symbol, start)

    # Find our specific contracts (ATM call and put at our strike)
    # ... this is simplified, you'd need to match exact contracts

    # Get historical prices
    history = qb.History(equity.Symbol, start, end, Resolution.Daily)

    daily_data = []
    current_date = start
    peak_pnl_pct = 0
    peak_date = start

    while current_date <= end:
        # Get option prices for this date
        # Calculate straddle value
        # Calculate P&L vs entry

        # This is a placeholder - actual implementation needs option price history
        pnl_pct = 0  # Calculate from option prices

        if pnl_pct > peak_pnl_pct:
            peak_pnl_pct = pnl_pct
            peak_date = current_date

        daily_data.append({
            "date": current_date,
            "days_held": (current_date - start).days,
            "pnl_pct": pnl_pct,
            "peak_pnl_pct": peak_pnl_pct,
        })

        current_date += timedelta(days=1)

    return pd.DataFrame(daily_data)

# %% [markdown]
# ## 3. Alternative: Use ORATS Data
# If you have ORATS historical options data, it's easier to use that directly.

# %%
def analyze_with_orats(orats_file, trades_csv):
    """
    Use ORATS historical data to reconstruct P&L trajectories.
    ORATS has daily option prices which makes this straightforward.

    Parameters:
    - orats_file: Path to ORATS EOD options data
    - trades_csv: Path to lifecycle_trades.csv with entry/exit info
    """
    # Load trades
    trades_df = pd.read_csv(trades_csv)

    # Load ORATS data (you have 2007-2025)
    # orats_df = pd.read_csv(orats_file)

    results = []

    for _, trade in trades_df.iterrows():
        symbol = trade["symbol"]
        entry_date = trade["entry_date"]
        exit_date = trade["exit_date"]

        # Filter ORATS data for this symbol and date range
        # Get daily straddle prices
        # Calculate P&L trajectory
        # Find peak

        # Placeholder result
        results.append({
            "symbol": symbol,
            "entry_date": entry_date,
            "exit_date": exit_date,
            "exit_pnl_pct": trade["pnl_pct"],
            "peak_pnl_pct": 0,  # Calculate from ORATS
            "peak_date": "",    # Find from trajectory
            "capture_rate": 0,  # exit / peak * 100
            "days_from_peak": 0,
        })

    return pd.DataFrame(results)

# %% [markdown]
# ## 4. Recommended Approach
#
# Since you have ORATS EOD data (2007-2025), the cleanest approach is:
#
# 1. Export the trade list (lifecycle_trades.csv) - DONE
# 2. Load ORATS data for those symbols and date ranges
# 3. Reconstruct daily straddle prices
# 4. Calculate P&L trajectory and find peak
# 5. Calculate capture rate = exit_pnl / peak_pnl
#
# This can be done in a Python script locally using your ORATS data,
# rather than in QC Research.

# %%
print("Lifecycle Research Notebook")
print("="*50)
print("To analyze capture rate:")
print("1. Load lifecycle_trades.csv (199 trades)")
print("2. For each trade, get daily option prices from ORATS")
print("3. Calculate daily P&L: (straddle_price / entry_price - 1) * 100")
print("4. Find peak P&L and capture rate")
print("")
print("Since you have ORATS data locally, run this analysis")
print("in a local Python script rather than QC Research.")
