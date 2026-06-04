"""Convert iQuant/RQData market data into Lean's local data layout.

This tool is intentionally offline-only: Lean backtests read the files it
produces and never import or call iQuant at runtime.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import sys
import zipfile
from dataclasses import dataclass
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path
from typing import Iterable


WIND_SUFFIX_TO_MARKET = {
    "SH": "sh",
    "SZ": "sz",
    "CFE": "cffex",
    "SHF": "shf",
    "DCE": "dce",
    "CZC": "czc",
    "INE": "ine",
}


@dataclass(frozen=True)
class WindSymbol:
    value: str
    ticker: str
    wind_suffix: str
    lean_market: str

    @property
    def lean_ticker(self) -> str:
        return self.ticker.lower() if self.lean_market not in {"sh", "sz"} else self.ticker

    @property
    def lean_file_stem(self) -> str:
        return self.lean_ticker.lower()

    @staticmethod
    def parse(value: str) -> "WindSymbol":
        if "." not in value:
            raise ValueError(f"Wind symbol must include an exchange suffix: {value}")
        ticker, suffix = value.rsplit(".", 1)
        suffix = suffix.upper()
        if suffix not in WIND_SUFFIX_TO_MARKET:
            raise ValueError(f"Unsupported Wind exchange suffix '{suffix}' in {value}")
        if not ticker:
            raise ValueError(f"Wind symbol is missing a ticker: {value}")
        return WindSymbol(value=value, ticker=ticker.upper(), wind_suffix=suffix, lean_market=WIND_SUFFIX_TO_MARKET[suffix])


def scale_price(value: object) -> int:
    return int((Decimal(str(value)) * Decimal("10000")).quantize(Decimal("1"), rounding=ROUND_HALF_UP))


class ConversionManifest:
    def __init__(self, path: Path):
        self.path = path
        self.data = {"completed": {}, "files": {}}
        if path.exists():
            self.data.update(json.loads(path.read_text(encoding="utf-8")))

    def is_completed(self, key: str) -> bool:
        return key in self.data["completed"]

    def mark_completed(self, key: str) -> None:
        self.data["completed"][key] = True
        self.save()

    def record_file(self, path: Path) -> None:
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        self.data["files"][str(path)] = {"sha256": digest, "bytes": path.stat().st_size}
        self.save()

    def save(self) -> None:
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.path.write_text(json.dumps(self.data, indent=2, sort_keys=True), encoding="utf-8")


class IQuantRqDataAdapter:
    def __init__(self):
        from iquant.datafeed.rqdata.stock import RqQuotation as StockQuotation
        from iquant.datafeed.rqdata.fund import RqQuotation as FundQuotation
        from iquant.datafeed.rqdata.future import RqContract, RqQuotation as FutureQuotation
        from iquant.type import Frequency

        self.stock_quotation = StockQuotation()
        self.fund_quotation = FundQuotation()
        self.future_contract = RqContract()
        self.future_quotation = FutureQuotation()
        self.frequency_type = Frequency

    def get_prices(self, asset_type: str, symbols: list[str], start, end, frequency):
        fields = ["security_id", "date_time", "open", "high", "low", "close", "volume"]
        frequency_value = getattr(self.frequency_type, frequency.upper())
        if asset_type in {"equity", "stock"}:
            return self.stock_quotation.get_price(symbols, fields, start, end, frequency_value, adjusted=None)
        if asset_type == "fund":
            return self.fund_quotation.get_price(symbols, fields, start, end, frequency_value, adjusted=None)
        if asset_type == "future":
            fields.append("oi")
            return self.future_quotation.get_price(symbols, fields, start, end, frequency_value, adjusted=None)
        raise ValueError(f"Unsupported asset type: {asset_type}")


def lean_tradebar_rows(records: Iterable[dict]) -> list[str]:
    rows = []
    for record in records:
        timestamp = record["date_time"]
        if hasattr(timestamp, "strftime"):
            timestamp = timestamp.strftime("%Y%m%d %H:%M")
        rows.append(
            ",".join(
                [
                    str(timestamp),
                    str(scale_price(record["open"])),
                    str(scale_price(record["high"])),
                    str(scale_price(record["low"])),
                    str(scale_price(record["close"])),
                    str(int(record.get("volume", 0) or 0)),
                ]
            )
        )
    return rows


def write_zip_csv(path: Path, entry_name: str, rows: list[str], dry_run: bool) -> None:
    if dry_run:
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        archive.writestr(entry_name, "\n".join(rows) + ("\n" if rows else ""))


def write_identity_factor_file(data_folder: Path, symbol: WindSymbol, dry_run: bool) -> Path:
    path = data_folder / "equity" / symbol.lean_market / "factor_files" / f"{symbol.lean_file_stem}.csv"
    if not dry_run:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("20501231,1,1,0\n", encoding="utf-8")
    return path


def write_map_file(data_folder: Path, symbol: WindSymbol, dry_run: bool) -> Path:
    path = data_folder / "equity" / symbol.lean_market / "map_files" / f"{symbol.lean_file_stem}.csv"
    if not dry_run:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(f"20501231,{symbol.lean_file_stem}\n", encoding="utf-8")
    return path


def convert_from_csv(input_csv: Path, data_folder: Path, asset_type: str, frequency: str, dry_run: bool, manifest: ConversionManifest) -> None:
    with input_csv.open(newline="", encoding="utf-8") as handle:
        records = list(csv.DictReader(handle))
    if not records:
        return

    symbol = WindSymbol.parse(records[0]["security_id"])
    key = f"{asset_type}:{symbol.value}:{frequency}:{input_csv}"
    if manifest.is_completed(key):
        return

    rows = lean_tradebar_rows(records)
    security_folder = "future" if asset_type == "future" else "equity"
    zip_path = data_folder / security_folder / symbol.lean_market / frequency.lower() / f"{symbol.lean_file_stem}.zip"
    write_zip_csv(zip_path, f"{symbol.lean_file_stem}.csv", rows, dry_run)

    if asset_type != "future":
        write_identity_factor_file(data_folder, symbol, dry_run)
        write_map_file(data_folder, symbol, dry_run)

    if not dry_run:
        manifest.record_file(zip_path)
    manifest.mark_completed(key)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Convert iQuant/RQData output to Lean local data files.")
    parser.add_argument("--data-folder", required=True, type=Path)
    parser.add_argument("--manifest", type=Path)
    parser.add_argument("--input-csv", type=Path, help="Offline CSV input with security_id,date_time,open,high,low,close,volume columns.")
    parser.add_argument("--asset-type", choices=["equity", "fund", "future"], default="equity")
    parser.add_argument("--frequency", choices=["daily", "minute", "tick"], default="daily")
    parser.add_argument("--dry-run", action="store_true")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    manifest_path = args.manifest or args.data_folder / "iquant-conversion-manifest.json"
    manifest = ConversionManifest(manifest_path)
    if args.input_csv is None:
        print("Only --input-csv conversion is implemented in this scaffold. iQuant live pulls plug into IQuantRqDataAdapter.", file=sys.stderr)
        return 2
    convert_from_csv(args.input_csv, args.data_folder, args.asset_type, args.frequency, args.dry_run, manifest)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
