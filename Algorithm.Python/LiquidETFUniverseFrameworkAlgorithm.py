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
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Brokerages import *
from QuantConnect.Data import *
from QuantConnect.Data.UniverseSelection import *
from datetime import timedelta

### <summary>
### Basic template framework algorithm uses framework components to define the algorithm.
### Liquid ETF Competition template
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class LiquidETFUniverseFrameworkAlgorithm(QCAlgorithm):
    '''Basic template framework algorithm uses framework components to define the algorithm.'''

    def Initialize(self):
        # Set Start Date so that backtest has 5+ years of data
        self.SetStartDate(2014, 11, 1)
        # No need to set End Date as the final submission will be tested
        # up until the review date

        # Set $1m Strategy Cash to trade significant AUM
        self.SetCash(1000000)

        # Add a relevant benchmark, with the default being SPY
        self.SetBenchmark('SPY')

        # Use the Alpha Streams Brokerage Model, developed in conjunction with
        # funds to model their actual fees, costs, etc.
        # Please do not add any additional reality modelling, such as Slippage, Fees, Buying Power, etc.
        self.SetBrokerageModel(AlphaStreamsBrokerageModel())

        # Use the LiquidETFUniverse with minute-resolution data
        self.UniverseSettings.Resolution = Resolution.Minute
        self.SetUniverseSelection(LiquidETFUniverse())

        # Optional
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())

        # List of symbols we want to trade. Set it in OnSecuritiesChanged
        self.symbols = []

    def OnData(self, slice):

        if all([self.Portfolio[x].Invested for x in self.symbols]):
            return

        # Emit insights
        insights = [Insight.Price(x, timedelta(1), InsightDirection.Up)
            for x in self.symbols if self.Securities[x].Price > 0]

        if len(insights) > 0:
            self.EmitInsights(insights)

    def OnSecuritiesChanged(self, changes):

        # Set symbols as the Inverse Energy ETFs
        for security in changes.AddedSecurities:
            if security.Symbol in LiquidETFUniverse.Energy.Inverse:
                self.symbols.append(security.Symbol)

        # Print out the information about the groups
        self.Log(f'Energy: {LiquidETFUniverse.Energy}')
        self.Log(f'Metals: {LiquidETFUniverse.Metals}')
        self.Log(f'Technology: {LiquidETFUniverse.Technology}')
        self.Log(f'Treasuries: {LiquidETFUniverse.Treasuries}')
        self.Log(f'Volatility: {LiquidETFUniverse.Volatility}')
        self.Log(f'SP500Sectors: {LiquidETFUniverse.SP500Sectors}')