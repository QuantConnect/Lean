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

# region imports
from AlgorithmImports import *
from Execution.ImmediateExecutionModel import ImmediateExecutionModel
from QuantConnect.Orders import OrderEvent
# endregion

### <summary>
### Regression algorithm to test ImmediateExecutionModel places orders with the
### correct quantity (taking into account the fee's) so that the fill quantity
### is the expected one.
### </summary>
class ImmediateExecutionModelWorksWithBinanceFeeModel(QCAlgorithm):

    def Initialize(self):
        # *** initial configurations and backtest ***
        self.set_start_date(2022, 12, 13)  # Set Start Date
        self.set_end_date(2022, 12, 14)  # Set End Date
        self.set_account_currency("BUSD") # Set Account Currency
        self.set_cash("BUSD", 100000, 1)  # Set Strategy Cash

        self.universe_settings.resolution = Resolution.MINUTE

        symbols = [ Symbol.create("BTCBUSD", SecurityType.CRYPTO, Market.BINANCE) ]

        # set algorithm framework models
        self.set_universe_selection(ManualUniverseSelectionModel(symbols))
        self.set_alpha(ConstantAlphaModel(InsightType.PRICE, InsightDirection.UP, timedelta(minutes = 20), 0.025, None))

        self.set_portfolio_construction(EqualWeightingPortfolioConstructionModel(Resolution.MINUTE))
        self.set_execution(ImmediateExecutionModel())
        
        
        self.set_brokerage_model(BrokerageName.BINANCE, AccountType.MARGIN)
    
    def on_order_event(self, order_event: OrderEvent) -> None:
        if order_event.status == OrderStatus.FILLED:
            if abs(order_event.quantity - 5.8) > 0.01:
                raise AssertionError(f"The expected quantity was 5.8 but the quantity from the order was {order_event.quantity}")
