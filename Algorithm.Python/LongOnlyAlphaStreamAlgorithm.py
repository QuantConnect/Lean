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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Brokerages import *
from QuantConnect.Interfaces import *
from QuantConnect.Orders import *
from System import *
from datetime import timedelta

### <summary>
### Basic template framework algorithm uses framework components to define the algorithm.
### Shows EqualWeightingPortfolioConstructionModel.LongOnly() application
### </summary>
### <meta name="tag" content="alpha streams" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="algorithm framework" />
class LongOnlyAlphaStreamAlgorithm(QCAlgorithm):
    '''Basic template framework algorithm uses framework components to define the algorithm.
    Shows EqualWeightingPortfolioConstructionModel.LongOnly() application'''

    def Initialize(self):

        # 1. Required: 
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)

        # 2. Required: Alpha Streams Models:
        self.SetBrokerageModel(BrokerageName.AlphaStreams)

        # 3. Required: Significant AUM Capacity
        self.SetCash(1000000)

        # Only SPY will be traded
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(Resolution.Daily, PortfolioBias.Long))
        self.SetExecution(ImmediateExecutionModel())

        # Set algorithm framework models
        self.SetUniverseSelection(ManualUniverseSelectionModel(
            [Symbol.Create(x, SecurityType.Equity, Market.USA) for x in ["SPY", "IBM"]]))

    def OnData(self, slice):

        if self.Portfolio.Invested: return

        self.EmitInsights(
            [
                Insight.Price("SPY", timedelta(1), InsightDirection.Up),
                Insight.Price("IBM", timedelta(1), InsightDirection.Down)
            ])

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            if self.Securities[orderEvent.Symbol].Holdings.IsShort:
                raise ValueError("Invalid position, should not be short");
            self.Debug(orderEvent)