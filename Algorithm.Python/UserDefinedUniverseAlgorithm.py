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
### This algorithm shows how you can handle universe selection in anyway you like,
### at any time you like. This algorithm has a list of 10 stocks that it rotates
### through every hour.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="custom universes" />
class UserDefinedUniverseAlgorithm(QCAlgorithm):

	def initialize(self) -> None:
		self.set_cash(100000)
		self.set_start_date(2015, 1, 1)
		self.set_end_date(2015, 12, 1)
		self._symbols = ["SPY", "GOOG", "IBM", "AAPL", "MSFT", "CSCO", "ADBE", "WMT"]

		self.universe_settings.resolution = Resolution.HOUR
		self.add_universe('my_universe_name', Resolution.HOUR, self.selection)

	def selection(self, time: datetime) -> Iterable[str]:
		index = time.hour % len(self._symbols)
		return [self._symbols[index]]

	def on_securities_changed(self, changes: SecurityChanges) -> None:
		for removed in changes.removed_securities:
			if removed.invested:
				self.liquidate(removed.symbol)

		for added in changes.added_securities:
			self.set_holdings(added.symbol, 1/len(changes.added_securities))
