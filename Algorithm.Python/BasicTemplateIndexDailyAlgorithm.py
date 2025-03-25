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
# limitations under the License

import datetime
from AlgorithmImports import *

class BasicTemplateIndexDailyAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2021, 1, 1)
        self.set_end_date(2021, 1, 18)
        self.set_cash(1000000)

        # Use indicator for signal; but it cannot be traded
        self.spx = self.add_index("SPX", Resolution.DAILY).symbol

        # Trade on SPX ITM calls
        self.spx_option = Symbol.create_option(
            self.spx,
            Market.USA,
            OptionStyle.EUROPEAN,
            OptionRight.CALL,
            3200,
            datetime(2021, 1, 15)
        )

        self.add_index_option_contract(self.spx_option, Resolution.DAILY)

        self.ema_slow = self.ema(self.spx, 80)
        self.ema_fast = self.ema(self.spx, 200)

        self.ExpectedBarCount = 10
        self.BarCounter = 0

        self.settings.daily_precise_end_time = True

    def on_data(self, data: Slice):
        if not self.portfolio.invested:
            # SPX Index is not tradable, but we can trade an option
            self.market_order(self.spx_option, 1)
        else:
            self.liquidate()

        # Count how many slices we receive with SPX data in it to assert later
        if data.contains_key(self.spx):
            self.BarCounter = self.BarCounter + 1

    def OnEndOfAlgorithm(self):
        if self.BarCounter != self.ExpectedBarCount:
            raise ValueError(f"Bar Count {self.BarCounter} is not expected count of {self.ExpectedBarCount}")

        for symbol in [ self.spx_option, self.spx ]:
            history = self.history(symbol, 10)
            if len(history) != 10:
                raise ValueError(f"Unexpected history count: {len(history)}")
            if any(x for x in history.index.get_level_values('time') if x.time() != time(15, 15, 0)):
                raise ValueError(f"Unexpected history data time")
