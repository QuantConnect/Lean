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
from QuantConnect.Securities.Option import OptionPriceModel, OptionPriceModelParameters

### <summary>
### Regression algorithm testing custom option price model implementation
### </summary>
class CustomOptionPriceModelRegressionAlgorithm(QCAlgorithm):
    '''Regression algorithm testing custom option price model implementation'''

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(100000)

        option = self.add_option("GOOG")
        self._option_symbol = option.symbol

        option.set_filter(lambda u: u.standards_only().strikes(-2, +2).expiration(0, 180))
        option.set_price_model(CustomOptionPriceModelPython())

    def on_data(self, slice):
        if self.portfolio.invested:
            return

        if slice.option_chains.contains_key(self._option_symbol):
            chain = slice.option_chains[self._option_symbol]
            
            for contract in chain.contracts.values():
                if (contract.theoretical_price > 0 and contract.last_price > 0 and contract.theoretical_price < contract.last_price * 0.9):
                    self.market_order(contract.symbol, 1)
                    break

class CustomOptionPriceModelPython():
    def evaluate(self, parameters):
        contract = parameters.contract
        underlying = contract.underlying_last_price
        strike = contract.strike
        
        if contract.right == OptionRight.CALL:
            intrinsic = max(0, underlying - strike)
        else:
            intrinsic = max(0, strike - underlying)
        
        theoretical_price = intrinsic + 1.0
        
        return OptionPriceModelResult(theoretical_price, SimpleGreeks())

class SimpleGreeks(Greeks):
    def __init__(self):
        # delta, gamma, vega, theta, rho, lambda_
        super().__init__(0.5, 0.1, 0.2, -0.05, 0.1, 2.0)
        # You can also assign the values individually if preferred:
        # self.delta = 0.5
        # self.gamma = 0.1
        # self.vega = 0.2
        # self.theta = -0.05
        # self.rho = 0.1
        # self.lambda_ = 2.0