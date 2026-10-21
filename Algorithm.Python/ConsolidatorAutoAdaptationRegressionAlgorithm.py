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

from AlgorithmImports import *

### <summary>
### Regression algorithm asserting that trade bar consolidators and indicators registered on a quote-only
### feed (forex) are fed quote bars collapsed into mid-point trade bars instead of being rejected, and that
### the collapsed bars carry no volume.
### </summary>
class ConsolidatorAutoAdaptationRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2014, 5, 5)
        self.set_end_date(2014, 5, 12)

        eurusd = self.add_forex("EURUSD", Resolution.MINUTE, Market.OANDA).symbol
        self._adapted_trade_bars = 0

        # a trade bar consolidator on a quote-only feed: the engine feeds it quote bars collapsed
        # into mid-point trade bars. Quote data carries no trade volume, so reading it throws
        self._consolidator = TradeBarConsolidator(timedelta(hours=1))
        self._consolidator.data_consolidated += self._on_trade_bar
        self.subscription_manager.add_consolidator(eurusd, self._consolidator)

        # a trade bar indicator on a quote-only feed is fed collapsed trade bars as well
        self._mass = self.mass(eurusd, resolution=Resolution.HOUR)

    def _on_trade_bar(self, sender, bar):
        self._adapted_trade_bars += 1
        try:
            volume = bar.volume
        except InvalidOperationException:
            # expected, quote data carries no trade volume
            return
        raise AssertionError("Expected reading the volume of a trade bar collapsed from quote bars to throw")

    def on_end_of_algorithm(self):
        if self._adapted_trade_bars == 0:
            raise AssertionError("Expected the adapted trade bar consolidator to receive data")
        if self._mass.samples == 0:
            raise AssertionError("Expected the MassIndex indicator to be updated with collapsed trade bars")
