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

from System import Action

from QuantConnect.Logging import *

### <summary>
### Algorithm asserting that when setting custom models for canonical securities, a one-time warning is sent
### informing the user that the contracts models are different (not the custom ones).
### </summary>
class OptionModelsConsistencyRegressionAlgorithm(QCAlgorithm):

    def Initialize(self) -> None:
        security = self.InitializeAlgorithm()
        self.SetModels(security)

        # Using a custom security initializer derived from BrokerageModelSecurityInitializer
        # to check that the models are correctly set in the security even when the
        # security initializer is derived from said class in Python
        self.SetSecurityInitializer(CustomSecurityInitializer(self.BrokerageModel, SecuritySeeder.Null))

        self.SetBenchmark(lambda x: 0)

    def InitializeAlgorithm(self) -> Security:
        self.SetStartDate(2015, 12, 24)
        self.SetEndDate(2015, 12, 24)

        equity = self.AddEquity("GOOG", leverage=4)
        option = self.AddOption(equity.Symbol)
        option.SetFilter(lambda u: u.Strikes(-2, +2).Expiration(0, 180))

        return option

    def SetModels(self, security: Security) -> None:
        security.SetFillModel(CustomFillModel())
        security.SetFeeModel(CustomFeeModel())
        security.SetBuyingPowerModel(CustomBuyingPowerModel())
        security.SetSlippageModel(CustomSlippageModel())
        security.SetVolatilityModel(CustomVolatilityModel())

class CustomSecurityInitializer(BrokerageModelSecurityInitializer):
    def __init__(self, brokerage_model: BrokerageModel, security_seeder: SecuritySeeder):
        super().__init__(brokerage_model, security_seeder)

class CustomFillModel(FillModel):
    pass

class CustomFeeModel(FeeModel):
    pass

class CustomBuyingPowerModel(BuyingPowerModel):
    pass

class CustomSlippageModel(ConstantSlippageModel):
    def __init__(self):
        super().__init__(0)

class CustomVolatilityModel(BaseVolatilityModel):
    pass
