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
### Regression algorithm asserting that the algorithm-level set_slippage_model() applies a custom
### SlippageModel subclass to all securities, with per-security models set afterwards taking precedence.
### It also asserts that BrokerageModelSecurityInitializer accepts a plain callable as security seeder
### and that None is accepted by the framework model setters as the null model.
### </summary>
class AlgorithmSlippageModelRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.set_cash(100000)

        # the security seeder can be a plain callable, it gets wrapped into a FuncSecuritySeeder
        self.set_security_initializer(BrokerageModelSecurityInitializer(self.brokerage_model, self.get_last_known_prices))

        # None is accepted by the framework model setters as the null model
        self.set_risk_management(None)
        self.set_execution(None)

        self._slippage_model = CustomSlippageModel()
        self.set_slippage_model(self._slippage_model)

        self._spy = self.add_equity("SPY", Resolution.MINUTE).symbol
        ibm = self.add_equity("IBM", Resolution.MINUTE)
        # per-security models set after the algorithm-level model take precedence for that security
        ibm.set_slippage_model(NullSlippageModel.INSTANCE)
        self._ibm = ibm.symbol

    def on_data(self, data):
        if not self.portfolio.invested:
            self.set_holdings(self._spy, 0.5)
            self.set_holdings(self._ibm, 0.5)

    def on_end_of_algorithm(self):
        if not isinstance(self.securities[self._ibm].slippage_model, NullSlippageModel):
            raise AssertionError("Expected the per-security slippage model to take precedence for IBM")
        if self._slippage_model.call_count == 0:
            raise AssertionError("Expected the algorithm-level slippage model to have been used")
        if not isinstance(self.risk_management, NullRiskManagementModel):
            raise AssertionError("Expected set_risk_management(None) to set the null risk management model")
        if not isinstance(self.execution, NullExecutionModel):
            raise AssertionError("Expected set_execution(None) to set the null execution model")

### <summary>
### Custom slippage model derived from the C# SlippageModel base class.
### The ISlippageModel interface cannot be used as a Python base class.
### </summary>
class CustomSlippageModel(SlippageModel):
    def __init__(self):
        super().__init__()
        self.call_count = 0

    def get_slippage_approximation(self, asset, order):
        self.call_count += 1
        return 0.05
