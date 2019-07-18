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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data.Market import TradeBar
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioTarget, EqualWeightingPortfolioConstructionModel
from QuantConnect.Orders.Fees import ConstantFeeModel
from QuantConnect.Orders.Slippage import ConstantSlippageModel
from datetime import datetime, timedelta

#
# In a perfect market, you could buy 100 EUR worth of USD, sell 100 EUR worth of GBP,
# and then use the GBP to buy USD and wind up with the same amount in USD as you received when
# you bought them with EUR. This relationship is expressed by the Triangle Exchange Rate, which is
#
#     Triangle Exchange Rate = (A/B) * (B/C) * (C/A)
#
# where (A/B) is the exchange rate of A-to-B. In a perfect market, TER = 1, and so when
# there is a mispricing in the market, then TER will not be 1 and there exists an arbitrage opportunity.
#
# This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha.
#

class TriangleExchangeRateArbitrageAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.SetStartDate(2019, 2, 1)   #Set Start Date
        self.SetCash(100000)           #Set Strategy Cash

        # Set zero transaction fees
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))

        ## Select trio of currencies to trade where
        ## Currency A = USD
        ## Currency B = EUR
        ## Currency C = GBP
        currencies = ['EURUSD','EURGBP','GBPUSD']
        symbols = [ Symbol.Create(currency, SecurityType.Forex, Market.Oanda) for currency in currencies]

        ## Manual universe selection with tick-resolution data
        self.UniverseSettings.Resolution = Resolution.Minute
        self.SetUniverseSelection( ManualUniverseSelectionModel(symbols) )

        self.SetAlpha(ForexTriangleArbitrageAlphaModel(Resolution.Minute, symbols))

        ## Set Equal Weighting Portfolio Construction Model
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

        ## Set Immediate Execution Model
        self.SetExecution(ImmediateExecutionModel())

        ## Set Null Risk Management Model
        self.SetRiskManagement(NullRiskManagementModel())


class ForexTriangleArbitrageAlphaModel(AlphaModel):

    def __init__(self, insight_resolution, symbols):
        self.insight_period = Time.Multiply(Extensions.ToTimeSpan(insight_resolution), 5)
        self.symbols = symbols

    def Update(self, algorithm, data):
        ## Check to make sure all currency symbols are present
        if len(data.Keys) < 3:
            return []

        ## Extract QuoteBars for all three Forex securities
        bar_a = data[self.symbols[0]]
        bar_b = data[self.symbols[1]]
        bar_c = data[self.symbols[2]]

        ## Calculate the triangle exchange rate
        ## Bid(Currency A -> Currency B) * Bid(Currency B -> Currency C) * Bid(Currency C -> Currency A)
        ## If exchange rates are priced perfectly, then this yield 1. If it is different than 1, then an arbitrage opportunity exists
        triangleRate = bar_a.Ask.Close / bar_b.Bid.Close / bar_c.Ask.Close

        ## If the triangle rate is significantly different than 1, then emit insights
        if triangleRate > 1.0005:
            return Insight.Group(
                [
                    Insight.Price(self.symbols[0], self.insight_period, InsightDirection.Up, 0.0001, None),
                    Insight.Price(self.symbols[1], self.insight_period, InsightDirection.Down, 0.0001, None),
                    Insight.Price(self.symbols[2], self.insight_period, InsightDirection.Up, 0.0001, None)
                ] )

        return []