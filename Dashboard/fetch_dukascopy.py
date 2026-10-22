"""
Download XAUUSD minute candles from Dukascopy and write them in LEAN's cfd/oanda format.

Writes Data/cfd/oanda/minute/xauusd/YYYYMMDD_quote.zip per day and merges hour and daily
bars into Data/cfd/oanda/{hour,daily}/xauusd.zip (rows inside the downloaded range are replaced).

Usage: python3 fetch_dukascopy.py [--days 90] [--data ../Data]
"""
import argparse
import io
import lzma
import struct
import time
import urllib.error
import urllib.request
import zipfile
from datetime import date, datetime, timedelta, timezone
from pathlib import Path

SYMBOL = "xauusd"
POINT = 1000  # Dukascopy stores XAUUSD prices as integers scaled by 1000
URL = "https://datafeed.dukascopy.com/datafeed/XAUUSD/{y}/{m:02d}/{d:02d}/{side}_candles_min_1.bi5"
HEADERS = {"User-Agent": "Mozilla/5.0", "Referer": "https://freeserv.dukascopy.com/"}


def fetch_side(day: date, side: str) -> dict[int, tuple]:
    """Returns {seconds since midnight UTC: (open, high, low, close, volume)}"""
    # Dukascopy months are zero based
    url = URL.format(y=day.year, m=day.month - 1, d=day.day, side=side)
    for attempt in range(8):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=HEADERS), timeout=30) as response:
                raw = response.read()
            break
        except urllib.error.HTTPError as err:
            if err.code == 404:
                return {}
            if (err.code != 429 and err.code < 500) or attempt == 7:
                raise
            time.sleep(15 * (attempt + 1))
        except (urllib.error.URLError, TimeoutError):
            if attempt == 7:
                raise
            time.sleep(15 * (attempt + 1))
    if not raw:
        return {}
    data = lzma.decompress(raw)
    candles = {}
    for offset in range(0, len(data), 24):
        seconds, o, c, low, high, volume = struct.unpack(">IIIIIf", data[offset:offset + 24])
        candles[seconds] = (o / POINT, high / POINT, low / POINT, c / POINT, volume)
    return candles


def read_minute_zip(path: Path) -> tuple[dict, dict, list[int]]:
    bid, ask = {}, {}
    with zipfile.ZipFile(path) as z:
        for line in z.read(z.namelist()[0]).decode().splitlines():
            v = line.split(",")
            s = int(v[0]) // 1000
            bid[s] = (*map(float, v[1:5]), 1.0)
            ask[s] = (*map(float, v[6:10]), 1.0)
    return bid, ask, sorted(bid)


def fmt(x: float) -> str:
    return f"{x:.3f}".rstrip("0").rstrip(".")


def bar_row(prefix: str, bid: tuple, ask: tuple) -> str:
    # LEAN quote bar: bid OHLC, bid size, ask OHLC, ask size
    return ",".join([prefix, *map(fmt, bid[:4]), "0", *map(fmt, ask[:4]), "0"])


def aggregate(bars: list[tuple]) -> tuple:
    return (bars[0][0], max(b[1] for b in bars), min(b[2] for b in bars), bars[-1][3], sum(b[4] for b in bars))


def merge_zip(path: Path, csv_name: str, new_rows: dict[str, str], start: str):
    """Keep existing rows before `start`, replace the rest with new_rows"""
    rows = {}
    if path.exists():
        with zipfile.ZipFile(path) as z:
            for line in z.read(z.namelist()[0]).decode().splitlines():
                key = line.split(",", 1)[0]
                if key < start:
                    rows[key] = line
    rows.update(new_rows)
    path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as z:
        z.writestr(csv_name, "\n".join(rows[k] for k in sorted(rows)) + "\n")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--days", type=int, default=90)
    parser.add_argument("--data", default=str(Path(__file__).resolve().parent.parent / "Data"))
    args = parser.parse_args()

    root = Path(args.data) / "cfd" / "oanda"
    today = datetime.now(timezone.utc).date()
    first = today - timedelta(days=args.days)
    hours, days = {}, {}
    (root / "minute" / SYMBOL).mkdir(parents=True, exist_ok=True)

    day = first
    while day < today:  # today is incomplete
        stamp = day.strftime("%Y%m%d")
        minute_zip = root / "minute" / SYMBOL / f"{stamp}_quote.zip"
        if minute_zip.exists() and day < today - timedelta(days=1):
            # Already downloaded on an earlier run: reuse it so an interrupted download can resume
            bid, ask, seconds = read_minute_zip(minute_zip)
            fetched = False
        else:
            bid, ask = fetch_side(day, "BID"), fetch_side(day, "ASK")
            # Dukascopy pads closed market periods with flat zero volume candles; drop them
            seconds = sorted(s for s in bid if s in ask and (bid[s][4] > 0 or ask[s][4] > 0))
            fetched = True
        if seconds and fetched:
            lines = [bar_row(str(s * 1000), bid[s], ask[s]) for s in seconds]
            with zipfile.ZipFile(minute_zip, "w", zipfile.ZIP_DEFLATED) as z:
                z.writestr(f"{stamp}_{SYMBOL}_minute_quote.csv", "\n".join(lines) + "\n")
        if seconds:
            for hour in sorted({s // 3600 for s in seconds}):
                in_hour = [s for s in seconds if s // 3600 == hour]
                key = f"{stamp} {hour:02d}:00"
                hours[key] = bar_row(key, aggregate([bid[s] for s in in_hour]), aggregate([ask[s] for s in in_hour]))
            key = f"{stamp} 00:00"
            days[key] = bar_row(key, aggregate([bid[s] for s in seconds]), aggregate([ask[s] for s in seconds]))
        print(f"{day} {len(seconds)} bars{'' if fetched else ' (cached)'}")
        day += timedelta(days=1)
        if fetched:
            time.sleep(3)

    start_key = first.strftime("%Y%m%d")
    merge_zip(root / "hour" / f"{SYMBOL}.zip", f"{SYMBOL}.csv", hours, start_key)
    merge_zip(root / "daily" / f"{SYMBOL}.zip", f"{SYMBOL}.csv", days, start_key)
    print(f"done: {len(days)} trading days, {len(hours)} hourly bars")


if __name__ == "__main__":
    main()
