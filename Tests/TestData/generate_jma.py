"""
Generate JMA test data for QuantConnect/Lean.

Uses pandas_ta_classic as the reference implementation.
Reads the same SPY price data used by other indicator tests (spy_kama.txt)
and computes JMA(7, phase=0, power=2).

Usage:
    pip install pandas pandas-ta-classic
    python generate_jma.py
"""
import pandas as pd
import numpy as np

# Try pandas_ta_classic first, then pandas_ta
try:
    from pandas_ta_classic.overlap.jma import jma
except ImportError:
    from pandas_ta.overlap.jma import jma

# Read SPY data from the existing KAMA test file (same price data)
df = pd.read_csv("spy_kama.txt", parse_dates=["Date"])

# Compute JMA(7, phase=0) using pandas_ta reference
close = df["Close"].astype(float)
jma_values = jma(close, length=7, phase=0)

# Format output matching Lean test data conventions
df["JMA_7"] = jma_values.apply(
    lambda x: f"{x:.12f}" if not np.isnan(x) else ""
)

# Write output
output_cols = ["Date", "Open", "High", "Low", "Close", "Volume", "JMA_7"]
# Format dates to match Lean convention: "M/d/yyyy 12:00:00 AM"
df["Date"] = df["Date"].dt.strftime("%#m/%#d/%Y 12:00:00 AM")

# Format numeric columns to avoid trailing .0
for col in ["Open", "High", "Low", "Close"]:
    df[col] = df[col].apply(lambda x: f"{x:g}")
df["Volume"] = df["Volume"].astype(int)

df[output_cols].to_csv("spy_jma.txt", index=False)

print(f"Generated spy_jma.txt with {len(df)} rows")
print(f"First JMA value at row {jma_values.first_valid_index()}")
print(f"Sample values:")
valid = jma_values.dropna()
for i in range(min(5, len(valid))):
    idx = valid.index[i]
    print(f"  Row {idx}: Close={close.iloc[idx]:.2f}, JMA={valid.iloc[i]:.12f}")
