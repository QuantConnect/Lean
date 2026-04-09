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

from AlgorithmImports import *

### <summary>
### Demonstrates consuming optional Adanos market sentiment snapshots as custom equity data.
### This example is intended for live or research workflows that query current structured sentiment
### from Adanos. Historical backtests should first persist daily snapshots to a local file or ObjectStore.
### Parameters:
### - adanos-api-key: required to enable requests
### - adanos-source: one of news, reddit, x, polymarket
### - adanos-lookback-days: lookback window for the stock sentiment endpoint
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="alternative data" />
### <meta name="tag" content="sentiment" />
class AdanosMarketSentimentAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2024, 1, 1)
        self.set_end_date(2024, 1, 31)
        self.set_cash(100000)

        api_key = self.get_parameter("adanos-api-key")
        if not api_key:
            self.debug("Set the 'adanos-api-key' parameter to enable this optional custom data example.")
            self.quit()
            return

        source_name = self.get_parameter("adanos-source", "news")
        lookback_days = int(self.get_parameter("adanos-lookback-days", 30))
        AdanosSentiment.configure(api_key, source_name, lookback_days)

        self._equity_symbol = self.add_equity("AAPL", Resolution.DAILY).symbol
        self._sentiment_symbol = self.add_data(AdanosSentiment, self._equity_symbol, Resolution.DAILY).symbol

    def on_data(self, data: Slice) -> None:
        if not data.contains_key(self._sentiment_symbol):
            return

        sentiment = data[self._sentiment_symbol]
        buzz_score = float(sentiment.value)
        bullish_percent = float(sentiment["BullishPercent"])
        sentiment_score = float(sentiment["SentimentScore"])

        self.set_runtime_statistic("Adanos Buzz", f"{buzz_score:.1f}")
        self.set_runtime_statistic("Adanos Bullish %", f"{bullish_percent:.0f}")

        if not self.portfolio.invested and buzz_score >= 60 and bullish_percent >= 55 and sentiment_score > 0:
            self.set_holdings(self._equity_symbol, 1)
        elif self.portfolio.invested and (buzz_score < 40 or sentiment_score < 0):
            self.liquidate(self._equity_symbol)


class AdanosSentiment(PythonData):
    _api_key = None
    _lookback_days = 30
    _source_name = "news"
    _supported_sources = {"news", "reddit", "x", "polymarket"}

    @classmethod
    def configure(cls, api_key: str, source_name: str = "news", lookback_days: int = 30) -> None:
        cls._api_key = api_key
        normalized_source = (source_name or "news").lower()
        cls._source_name = normalized_source if normalized_source in cls._supported_sources else "news"
        cls._lookback_days = max(1, int(lookback_days))

    def get_source(self, config: SubscriptionDataConfig, date: datetime, is_live_mode: bool) -> SubscriptionDataSource:
        ticker = self._resolve_ticker(config.symbol)
        url = (
            f"https://api.adanos.org/{self._source_name}/stocks/v1/stock/"
            f"{ticker}?days={self._lookback_days}"
        )
        headers = {"X-API-Key": self._api_key}
        return SubscriptionDataSource(url, SubscriptionTransportMedium.REST, FileFormat.CSV, headers)

    def reader(self, config: SubscriptionDataConfig, line: str, date: datetime, is_live_mode: bool) -> BaseData:
        if not line:
            return None

        try:
            payload = json.loads(line)
        except ValueError:
            return None

        if not payload.get("found"):
            return None

        point = AdanosSentiment()
        point.symbol = config.symbol

        point.time = self._extract_time(payload.get("daily_trend"), date)
        point.end_time = point.time + timedelta(days=1)
        point.value = float(payload.get("buzz_score") or 0.0)
        point["BuzzScore"] = float(payload.get("buzz_score") or 0.0)
        point["SentimentScore"] = float(payload.get("sentiment_score") or 0.0)
        point["BullishPercent"] = int(payload.get("bullish_pct") or 0)
        point["ActivityCount"] = self._activity_count(payload)
        point["CoverageCount"] = self._coverage_count(payload)
        point["Trend"] = payload.get("trend") or "unknown"
        point["Ticker"] = payload.get("ticker") or self._resolve_ticker(config.symbol)
        return point

    @staticmethod
    def _resolve_ticker(symbol: Symbol) -> str:
        if getattr(symbol, "has_underlying", False) and symbol.underlying is not None:
            return symbol.underlying.value.upper()
        return symbol.value.upper()

    @staticmethod
    def _extract_time(daily_trend, fallback: datetime) -> datetime:
        if daily_trend:
            first = daily_trend[0]
            date_str = first.get("date")
            if date_str:
                return datetime.strptime(date_str, "%Y-%m-%d")
        return fallback

    @staticmethod
    def _activity_count(payload: dict) -> int:
        return int(
            payload.get("mentions")
            or payload.get("trade_count")
            or payload.get("unique_tweets")
            or 0
        )

    @staticmethod
    def _coverage_count(payload: dict) -> int:
        return int(
            payload.get("source_count")
            or payload.get("subreddit_count")
            or payload.get("market_count")
            or 0
        )
