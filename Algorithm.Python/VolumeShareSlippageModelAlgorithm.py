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

class VolumeShareSlippageModelAlgorithm(QCAlgorithm):
    longs = []
    shorts = []

    def initialize(self) -> None:
        self.set_start_date(2021, 1, 1)
        self.set_end_date(2022, 1, 1)
        # To set the slippage model to limit to fill only 30% volume of the historical volume, with 5% slippage impact.
        self.set_security_initializer(lambda security: security.set_slippage_model(VolumeShareSlippageModel(0.3, 0.05)))

        # Request extended market hour SPY data for trading.
        qqq = self.add_equity("QQQ").symbol
        
        # Weekly updating the portfolio to allow time to capitalize from the popularity gap.
        self.universe_settings.schedule.on(self.date_rules.week_start())
        # Add universe to trade on the most and least liquid stocks among QQQ constituents.
        self.add_universe(
            self.universe.etf(qqq, Market.USA, self.universe_settings, lambda constituents: [c.symbol for c in constituents]),
            self.fundamental_selection
        )
        
        # Set a schedule event to rebalance the portfolio every week start.
        self.schedule.on(
            self.date_rules.week_start(qqq),
            self.time_rules.after_market_open(qqq),
            self.rebalance
        )

    def fundamental_selection(self, fundamentals: List[Fundamental]) -> List[Symbol]:
        sorted_by_dollar_volume = sorted(fundamentals, key=lambda f: f.dollar_volume)
        # Add the 10 most liquid stocks to the universe to long later.
        self.longs = [f.symbol for f in sorted_by_dollar_volume[-10:]]
        # Add the 10 least liquid stocks to the universe to short later.
        self.shorts = [f.symbol for f in sorted_by_dollar_volume[:10]]

        return self.longs + self.shorts

    def rebalance(self) -> None:
        # Equally invest into the selected stocks to evenly dissipate capital risk.
        # Dollar neutral of long and short stocks to eliminate systematic risk, only capitalize the popularity gap.
        targets = [PortfolioTarget(symbol, 0.05) for symbol in self.longs]
        targets += [PortfolioTarget(symbol, -0.05) for symbol in self.shorts]

        # Liquidate the ones not being the most and least popularity stocks to release fund for higher expected return trades.
        self.set_holdings(targets, liquidate_existing_holdings=True)