#!/usr/bin/env python
"""Run local China-market Lean backtests from a plain Python command."""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parent

ALGORITHMS = {
    "equity": {
        "type_name": "ChinaEquityMovingAverageCrossAlgorithm",
        "location": ROOT / "Algorithm.Python" / "ChinaEquityMovingAverageCrossAlgorithm.py",
    },
    "portfolio": {
        "type_name": "ChinaEquityPortfolioAlgorithm",
        "location": ROOT / "Algorithm.Python" / "ChinaEquityPortfolioAlgorithm.py",
    },
    "minute": {
        "type_name": "ChinaEquityMinuteAlgorithm",
        "location": ROOT / "Algorithm.Python" / "ChinaEquityMinuteAlgorithm.py",
    },
    "tick": {
        "type_name": "ChinaEquityTickAlgorithm",
        "location": ROOT / "Algorithm.Python" / "ChinaEquityTickAlgorithm.py",
    },
    "future": {
        "type_name": "ChinaFutureMovingAverageCrossAlgorithm",
        "location": ROOT / "Algorithm.Python" / "ChinaFutureMovingAverageCrossAlgorithm.py",
    },
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run a Lean China-market Python backtest.")
    parser.add_argument(
        "algorithm",
        choices=sorted(ALGORITHMS),
        nargs="?",
        default="equity",
        help="Algorithm to run. Defaults to equity.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    algorithm = ALGORITHMS[args.algorithm]

    command = [
        "dotnet",
        "run",
        "--project",
        str(ROOT / "Launcher" / "QuantConnect.Lean.Launcher.csproj"),
        "--",
        "--algorithm-type-name",
        algorithm["type_name"],
        "--algorithm-language",
        "Python",
        "--algorithm-location",
        str(algorithm["location"]),
        "--environment",
        "backtesting",
        "--data-folder",
        str(ROOT / "Data"),
    ]

    return subprocess.call(command, cwd=ROOT)


if __name__ == "__main__":
    sys.exit(main())
