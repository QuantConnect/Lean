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
from QuantConnect.Orders import *
from QuantConnect.Securities import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Selection import *
from Alphas.ConstantAlphaModel import ConstantAlphaModel
from Selection.FutureUniverseSelectionModel import FutureUniverseSelectionModel
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Risk import *
from datetime import date, timedelta

### <summary>
### Basic template futures framework algorithm uses framework components
### to define an algorithm that trades futures.
### </summary>
class BasicTemplateFuturesFrameworkAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.UniverseSettings.Resolution = Resolution.Minute

        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(100000)

        # set framework models
        self.SetUniverseSelection(FrontMonthFutureUniverseSelectionModel(self.SelectFutureChainSymbols))
        self.SetAlpha(ConstantFutureContractAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(1)))
        self.SetPortfolioConstruction(SingleSharePortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())


    def SelectFutureChainSymbols(self, utcTime):
        newYorkTime = Extensions.ConvertFromUtc(utcTime, TimeZones.NewYork)
        ticker = Futures.Indices.SP500EMini if newYorkTime.date() < date(2013, 10, 9) else Futures.Metals.Gold
        return [ Symbol.Create(ticker, SecurityType.Future, Market.USA) ]

class FrontMonthFutureUniverseSelectionModel(FutureUniverseSelectionModel):
    '''Creates futures chain universes that select the front month contract and runs a user
    defined futureChainSymbolSelector every day to enable choosing different futures chains'''
    def __init__(self, select_future_chain_symbols):
        super().__init__(timedelta(1), select_future_chain_symbols)

    def Filter(self, filter):
        '''Defines the futures chain universe filter'''
        return (filter.FrontMonth()
                      .OnlyApplyFilterAtMarketOpen())

class ConstantFutureContractAlphaModel(ConstantAlphaModel):
    '''Implementation of a constant alpha model that only emits insights for future symbols'''
    def __init__(self, type, direction, period):
        super().__init__(type, direction, period)

    def ShouldEmitInsight(self, utcTime, symbol):
        # only emit alpha for future symbols and not underlying equity symbols
        if symbol.SecurityType != SecurityType.Future:
            return False

        return super().ShouldEmitInsight(utcTime, symbol)

class SingleSharePortfolioConstructionModel(PortfolioConstructionModel):
    '''Portfolio construction model that sets target quantities to 1 for up insights and -1 for down insights'''
    def CreateTargets(self, algorithm, insights):
        targets = []
        for insight in insights:
            targets.append(PortfolioTarget(insight.Symbol, insight.Direction))
        return targets