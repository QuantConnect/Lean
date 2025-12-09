"""
Lifecycle Log Parser
====================
Parses SNAPSHOT and EXIT_SUMMARY logs from QuantConnect backtests.
Exports to CSV for Excel analysis.

Usage:
    python parse_lifecycle_logs.py [backtest_id]

Output:
    - daily_snapshots.csv: Every day of every trade
    - trade_summaries.csv: One row per trade with exit metrics
"""

import hashlib
import time
import requests
import json
import re
import csv
from collections import defaultdict
from datetime import datetime

# QuantConnect API credentials
USER_ID = "444200"
API_TOKEN = "8dce7bf6c29a042ee5700bcbeaaf89dcf54865d2c3b7bab5513436f7baba70c7"
PROJECT_ID = "26652712"

# Default to latest lifecycle backtest
DEFAULT_BACKTEST_ID = "90594ff3519796337ebbaa745c11fd06"


def get_auth():
    """Generate QC API authentication."""
    timestamp = str(int(time.time()))
    hashed = hashlib.sha256(f"{API_TOKEN}:{timestamp}".encode('utf-8')).hexdigest()
    return (USER_ID, hashed), {"Timestamp": timestamp}


def fetch_logs(backtest_id):
    """Fetch all logs from a backtest."""
    auth, headers = get_auth()
    url = "https://www.quantconnect.com/api/v2/backtests/read"
    params = {"projectId": PROJECT_ID, "backtestId": backtest_id}

    resp = requests.get(url, params=params, auth=auth, headers=headers)
    data = resp.json()

    if not data.get("success"):
        print(f"Error fetching backtest: {data}")
        return []

    logs = data.get("backtest", {}).get("logs", [])
    return logs


def parse_snapshot(log_line):
    """Parse a SNAPSHOT log line into a dict."""
    # Format: SNAPSHOT SYMBOL | {json}
    match = re.match(r'SNAPSHOT (\w+) \| (.+)', log_line)
    if not match:
        return None

    symbol = match.group(1)
    try:
        data = json.loads(match.group(2))
        data["symbol"] = symbol
        return data
    except json.JSONDecodeError:
        return None


def parse_exit_summary(log_line):
    """Parse an EXIT_SUMMARY log line into a dict."""
    # Format: EXIT_SUMMARY SYMBOL | Key=Value | Key=Value | ...
    match = re.match(r'EXIT_SUMMARY (\w+) \| (.+)', log_line)
    if not match:
        return None

    symbol = match.group(1)
    parts = match.group(2).split(" | ")

    data = {"symbol": symbol}
    for part in parts:
        if "=" in part:
            key, value = part.split("=", 1)
            # Clean up percentage signs and convert numbers
            value = value.strip()
            if value.endswith("%"):
                try:
                    data[key] = float(value[:-1])
                except ValueError:
                    data[key] = value
            else:
                try:
                    data[key] = float(value) if "." in value else int(value)
                except ValueError:
                    data[key] = value

    return data


def export_to_csv(snapshots, summaries, output_prefix="lifecycle"):
    """Export parsed data to CSV files."""

    # Export snapshots
    if snapshots:
        snapshot_file = f"{output_prefix}_daily_snapshots.csv"
        fieldnames = ["symbol", "date", "days_held", "pnl_pct", "peak_pnl_pct",
                      "current_iv", "entry_iv", "iv_ratio", "rsi", "adx",
                      "delta", "gamma", "theta", "vega",
                      "vega_guard", "rsi_ob", "delta_drift"]

        with open(snapshot_file, 'w', newline='') as f:
            writer = csv.DictWriter(f, fieldnames=fieldnames, extrasaction='ignore')
            writer.writeheader()
            for s in snapshots:
                writer.writerow(s)

        print(f"Wrote {len(snapshots)} snapshots to {snapshot_file}")

    # Export summaries
    if summaries:
        summary_file = f"{output_prefix}_trade_summaries.csv"
        fieldnames = ["symbol", "Entry", "Exit", "Days", "PeakPnL", "PeakDate",
                      "ExitPnL", "CaptureRate", "DaysFromPeak", "Reason", "IVR@Entry"]

        with open(summary_file, 'w', newline='') as f:
            writer = csv.DictWriter(f, fieldnames=fieldnames, extrasaction='ignore')
            writer.writeheader()
            for s in summaries:
                writer.writerow(s)

        print(f"Wrote {len(summaries)} trade summaries to {summary_file}")


def analyze_capture_rates(summaries):
    """Analyze TRUE capture rates from trade summaries."""
    print("\n" + "="*60)
    print("CAPTURE RATE ANALYSIS")
    print("="*60)

    # Filter to trades that had positive peaks
    trades_with_peaks = [s for s in summaries if s.get("PeakPnL", 0) > 0]

    if not trades_with_peaks:
        print("No trades with positive peaks found.")
        return

    # Calculate proper capture rate (only for trades with positive peaks)
    total_peak = sum(s["PeakPnL"] for s in trades_with_peaks)
    total_exit = sum(s.get("ExitPnL", 0) for s in trades_with_peaks)

    print(f"\nTrades with positive peaks: {len(trades_with_peaks)}")
    print(f"Total Peak P&L available: {total_peak:.1f}%")
    print(f"Total Exit P&L captured:  {total_exit:.1f}%")

    if total_peak > 0:
        overall_capture = (total_exit / total_peak) * 100
        print(f"Overall Capture Rate:     {overall_capture:.0f}%")
        print(f"Left on table:            {total_peak - total_exit:.1f}%")

    # By exit reason
    print("\nBy Exit Reason:")
    by_reason = defaultdict(list)
    for s in trades_with_peaks:
        by_reason[s.get("Reason", "UNKNOWN")].append(s)

    for reason, trades in sorted(by_reason.items()):
        peak_sum = sum(t["PeakPnL"] for t in trades)
        exit_sum = sum(t.get("ExitPnL", 0) for t in trades)
        capture = (exit_sum / peak_sum * 100) if peak_sum > 0 else 0
        avg_days_from_peak = sum(t.get("DaysFromPeak", 0) for t in trades) / len(trades)
        print(f"  {reason}: {len(trades)} trades | Peak={peak_sum:.1f}% | Exit={exit_sum:.1f}% | Capture={capture:.0f}% | Avg {avg_days_from_peak:.0f} days from peak")

    # Signal effectiveness
    print("\n" + "="*60)
    print("SIGNAL ANALYSIS (at time of exit)")
    print("="*60)

    # We need the last snapshot before exit for each trade
    # This requires matching snapshots to summaries by symbol and date


def main(backtest_id=None):
    """Main entry point."""
    if backtest_id is None:
        backtest_id = DEFAULT_BACKTEST_ID

    print(f"Fetching logs for backtest: {backtest_id}")
    logs = fetch_logs(backtest_id)

    if not logs:
        print("No logs found!")
        return

    print(f"Found {len(logs)} log entries")

    # Parse logs
    snapshots = []
    summaries = []
    entries = []

    for log in logs:
        if "SNAPSHOT" in log:
            parsed = parse_snapshot(log)
            if parsed:
                snapshots.append(parsed)
        elif "EXIT_SUMMARY" in log:
            parsed = parse_exit_summary(log)
            if parsed:
                summaries.append(parsed)
        elif "ENTRY" in log:
            entries.append(log)

    print(f"\nParsed:")
    print(f"  - {len(snapshots)} daily snapshots")
    print(f"  - {len(summaries)} trade exits")
    print(f"  - {len(entries)} trade entries")

    # Export to CSV
    export_to_csv(snapshots, summaries)

    # Analyze capture rates
    if summaries:
        analyze_capture_rates(summaries)


if __name__ == "__main__":
    import sys
    backtest_id = sys.argv[1] if len(sys.argv) > 1 else None
    main(backtest_id)
