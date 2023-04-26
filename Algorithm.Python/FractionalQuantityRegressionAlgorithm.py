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
### Regression algorithm for fractional forex pair
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="regression test" />
class FractionalQuantityRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.SetStartDate(2015, 11, 12)
        self.SetEndDate(2016, 4, 1)
        self.SetCash(100000)
        self.SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash)

        self.SetTimeZone(TimeZones.Utc)

        security = self.AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Daily, Market.GDAX, False, 1, True)

        ### The default buying power model for the Crypto security type is now CashBuyingPowerModel.
        ### Since this test algorithm uses leverage we need to set a buying power model with margin.
        security.SetBuyingPowerModel(SecurityMarginModel(3.3))

        con = TradeBarConsolidator(1)
        self.SubscriptionManager.AddConsolidator("BTCUSD", con)
        con.DataConsolidated += self.DataConsolidated
        self.SetBenchmark(security.Symbol)

    def DataConsolidated(self, sender, bar):
        quantity = math.floor((self.Portfolio.Cash + self.Portfolio.TotalFees) / abs(bar.Value + 1))
        btc_qnty = float(self.Portfolio["BTCUSD"].Quantity)

        if not self.Portfolio.Invested:
            self.Order("BTCUSD", quantity)
        elif btc_qnty == quantity:
            self.Order("BTCUSD", 0.1)
        elif btc_qnty == quantity + 0.1:
            self.Order("BTCUSD", 0.01)
        elif btc_qnty == quantity + 0.11:
            self.Order("BTCUSD", -0.02)
        elif btc_qnty == quantity + 0.09:
            # should fail (below minimum order quantity)
            self.Order("BTCUSD", 0.00001)
            self.SetHoldings("BTCUSD", -2.0)
            self.SetHoldings("BTCUSD", 2.0)
            self.Quit()
