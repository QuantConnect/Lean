# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

import json
from urllib.parse import quote

from AlgorithmImports import *


### <summary>
### Demonstrates importing FXMacroData macro release-calendar data as a LEAN custom data source.
### The public example uses USD CPI release-calendar rows and does not require an API key.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="macro data" />
### <meta name="tag" content="economic data" />
class FXMacroDataReleaseCalendarAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2026, 7, 1)
        self.set_end_date(2026, 7, 31)
        self.set_cash(100000)

        self._release = self.add_data(FXMacroDataReleaseCalendar, "inflation", Resolution.DAILY).symbol

    def on_data(self, data: Slice) -> None:
        if not data.contains_key(self._release):
            return

        release = data[self._release]
        self.debug(
            "FXMacroData {0} release at {1}; consensus={2}, forecast={3}".format(
                release["name"],
                release["announcement_datetime_local"],
                release["consensus"],
                release["forecast"],
            )
        )


class FXMacroDataReleaseCalendar(PythonData):
    """Public USD macro release calendar rows from FXMacroData."""

    _base_url = "https://api.fxmacrodata.com/v1/calendar/usd"

    def get_source(self, config: SubscriptionDataConfig, date: datetime, is_live_mode: bool) -> SubscriptionDataSource:
        indicator = quote(config.symbol.value.lower())
        request_date = date.strftime("%Y-%m-%d")
        source = (
            f"{self._base_url}?indicator={indicator}"
            f"&start_date={request_date}&end_date={request_date}"
        )
        headers = {"User-Agent": "Lean FXMacroData custom data example"}
        return SubscriptionDataSource(source, SubscriptionTransportMedium.REMOTE_FILE, FileFormat.CSV, headers)

    def reader(self, config: SubscriptionDataConfig, line: str, date: datetime, is_live_mode: bool) -> BaseData:
        if not line.strip():
            return None

        try:
            payload = json.loads(line)
            rows = payload.get("data") or []
            if not rows:
                return None

            row = rows[0]
            announcement_time = (
                row.get("announcement_datetime_local")
                or row.get("announcement_datetime_utc")
            )
            release_time = self._parse_iso_datetime(announcement_time)
            if release_time is None:
                release_time = datetime.strptime(row["date"], "%Y-%m-%d")

            release = FXMacroDataReleaseCalendar()
            release.symbol = config.symbol
            release.time = release_time
            release.end_time = release_time + timedelta(minutes=1)
            release.value = self._coerce_float(
                row.get("forecast"),
                row.get("consensus"),
                row.get("expected"),
                row.get("actual"),
                0,
            )

            release["name"] = row.get("name") or row.get("release") or config.symbol.value
            release["release"] = row.get("release") or config.symbol.value
            release["currency"] = payload.get("currency", "USD")
            release["source"] = row.get("source")
            release["observation_date"] = row.get("date")
            release["announcement_datetime_local"] = announcement_time
            release["consensus"] = self._coerce_float(row.get("consensus"), row.get("expected"))
            release["forecast"] = self._coerce_float(row.get("forecast"))
            release["market_tier"] = self._coerce_float(row.get("market_tier"), 0)
            release["release_date_confirmed"] = 1 if row.get("release_date_confirmed") else 0
            return release
        except (KeyError, TypeError, ValueError):
            return None

    @staticmethod
    def _parse_iso_datetime(value):
        if not value:
            return None
        try:
            return datetime.fromisoformat(value).replace(tzinfo=None)
        except ValueError:
            return None

    @staticmethod
    def _coerce_float(*values):
        for value in values:
            if value is None:
                continue
            try:
                return float(value)
            except (TypeError, ValueError):
                continue
        return None
