"""
Calculate TRUE Capture Rate Using ORATS Data
=============================================
For each trade in lifecycle_trades.csv:
1. Get daily option prices from ORATS
2. Calculate daily P&L trajectory
3. Find peak P&L
4. Calculate capture rate = exit_pnl / peak_pnl

Usage:
    python calculate_capture_rate.py --orats-path /path/to/orats/data

Requirements:
    - lifecycle_trades.csv (already created)
    - ORATS EOD data files
"""

import pandas as pd
import numpy as np
from datetime import datetime, timedelta
import os
import argparse

def load_trades(trades_file="lifecycle_trades.csv"):
    """Load trades from CSV."""
    df = pd.read_csv(trades_file)
    print(f"Loaded {len(df)} trades")
    return df

def get_orats_prices(orats_path, symbol, start_date, end_date, strike, expiry):
    """
    Get daily option prices from ORATS data.

    ORATS file format varies - adjust based on your actual data structure.
    Common columns: ticker, trade_date, expirDate, strike, call_bid, call_ask, put_bid, put_ask
    """
    # Placeholder - implement based on your ORATS data format
    # Example for ORATS Core data:
    #
    # orats_file = os.path.join(orats_path, f"{symbol}_options.csv")
    # df = pd.read_csv(orats_file)
    # df = df[(df['trade_date'] >= start_date) & (df['trade_date'] <= end_date)]
    # df = df[df['strike'] == strike]
    # df = df[df['expirDate'] == expiry]
    #
    # return df[['trade_date', 'call_mid', 'put_mid']]

    pass

def calculate_trajectory(daily_prices, entry_cost):
    """
    Calculate daily P&L trajectory.

    Returns DataFrame with:
    - date
    - straddle_value
    - pnl_pct
    - peak_pnl_pct (running max)
    - peak_date
    """
    trajectory = []
    peak_pnl = 0
    peak_date = None

    for _, row in daily_prices.iterrows():
        straddle_value = row['call_mid'] + row['put_mid']
        pnl_pct = (straddle_value / entry_cost - 1) * 100

        if pnl_pct > peak_pnl:
            peak_pnl = pnl_pct
            peak_date = row['date']

        trajectory.append({
            'date': row['date'],
            'straddle_value': straddle_value,
            'pnl_pct': pnl_pct,
            'peak_pnl_pct': peak_pnl,
            'peak_date': peak_date,
        })

    return pd.DataFrame(trajectory)

def analyze_trade(orats_path, trade):
    """Analyze a single trade and return capture rate metrics."""
    symbol = trade['symbol']
    entry_date = trade['entry_date']
    exit_date = trade['exit_date']
    entry_cost = trade['entry_cost']
    exit_pnl = trade['pnl_pct']

    # Get daily prices from ORATS
    daily_prices = get_orats_prices(
        orats_path, symbol, entry_date, exit_date,
        strike=None,  # Need to extract from trade data
        expiry=None   # Need to extract from trade data
    )

    if daily_prices is None or len(daily_prices) == 0:
        return None

    # Calculate trajectory
    trajectory = calculate_trajectory(daily_prices, entry_cost)

    # Get peak
    peak_row = trajectory[trajectory['pnl_pct'] == trajectory['pnl_pct'].max()].iloc[0]
    peak_pnl = peak_row['peak_pnl_pct']
    peak_date = peak_row['peak_date']

    # Calculate capture rate
    if peak_pnl > 0:
        capture_rate = (exit_pnl / peak_pnl) * 100
    else:
        capture_rate = 100 if exit_pnl <= 0 else 0

    exit_dt = datetime.strptime(exit_date, "%Y-%m-%d")
    peak_dt = datetime.strptime(peak_date, "%Y-%m-%d") if peak_date else exit_dt
    days_from_peak = (exit_dt - peak_dt).days

    return {
        'symbol': symbol,
        'entry_date': entry_date,
        'exit_date': exit_date,
        'exit_pnl_pct': exit_pnl,
        'peak_pnl_pct': peak_pnl,
        'peak_date': peak_date,
        'capture_rate': capture_rate,
        'days_from_peak': days_from_peak,
    }

def main():
    print("="*60)
    print("CAPTURE RATE CALCULATOR")
    print("="*60)
    print("")
    print("This script needs your ORATS data to calculate true capture rates.")
    print("")
    print("ORATS data format needed:")
    print("  - Daily option prices (bid/ask or mid)")
    print("  - Columns: ticker, trade_date, expirDate, strike, call_*, put_*")
    print("")
    print("Please provide the path to your ORATS data directory.")
    print("")
    print("Alternatively, you can use QC Research notebook which has")
    print("access to historical option prices via OptionChainProvider.")
    print("")
    print("="*60)
    print("")

    # Load trades
    trades = load_trades()
    print(trades.head())
    print("")
    print(f"Date range: {trades['entry_date'].min()} to {trades['exit_date'].max()}")
    print(f"Symbols: {trades['symbol'].unique().tolist()}")

if __name__ == "__main__":
    main()
