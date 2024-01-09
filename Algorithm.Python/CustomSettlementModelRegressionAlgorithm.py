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
from CustomBrokerageModelRegressionAlgorithm import CustomBrokerageModel

### <summary>
### Regression algorithm to test we can specify a custom settlement model, and override some of its methods
### </summary>
class CustomSettlementModelRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        self.SetCash(10000)
        self.spy = self.AddEquity("SPY", Resolution.Daily)
        self.SetSettlementModel(self.spy)

    def SetSettlementModel(self, security):
        self.SetBrokerageModel(CustomBrokerageModelWithCustomSettlementModel())

    def OnData(self, slice):
        if self.Portfolio.CashBook[Currencies.USD].Amount == 10000:
            parameters = ApplyFundsSettlementModelParameters(self.Portfolio, self.spy, self.Time, CashAmount(101, Currencies.USD), None)
            self.spy.SettlementModel.ApplyFunds(parameters)

    def OnEndOfAlgorithm(self):
        if self.Portfolio.CashBook[Currencies.USD].Amount != 10101:
            raise Exception(f"It was expected to have 10101 USD in Portfolio, but was {self.Portfolio.CashBook[Currencies.USD].Amount}")
        parameters = ScanSettlementModelParameters(self.Portfolio, self.spy, datetime(2013, 10, 6))
        self.spy.SettlementModel.Scan(parameters)
        if self.Portfolio.CashBook[Currencies.USD].Amount != 10000:
            raise Exception(f"It was expected to have 10000 USD in Portfolio, but was {self.Portfolio.CashBook[Currencies.USD].Amount}")

class CustomSettlementModel:
    def ApplyFunds(self, parameters):
        self.currency = parameters.CashAmount.Currency;
        self.amount = parameters.CashAmount.Amount
        parameters.Portfolio.CashBook[self.currency].AddAmount(self.amount)

    def Scan(self, parameters):
        if parameters.UtcTime == datetime(2013, 10, 6):
            parameters.Portfolio.CashBook[self.currency].AddAmount(-self.amount)

class CustomBrokerageModelWithCustomSettlementModel(CustomBrokerageModel):
    def GetSettlementModel(self, security):
        return CustomSettlementModel()
