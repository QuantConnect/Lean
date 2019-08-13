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
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Data.Custom.TradingEconomics import *

class InterestReleaseFrameworkAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2018, 1, 1)  # Set Start Date
        self.SetEndDate(2019, 4, 30)    # Set End Date
        self.SetCash(100000)           # Set Strategy Cash
        
        # custom data: Trading-Economics data
        symbols = [Symbol.Create(symbol, SecurityType.Forex, Market.FXCM) 
            for symbol in [
                "AUDUSD", "EURUSD", "NZDUSD", "GBPUSD",
                "USDCAD", "USDMXN", "USDJPY", "USDSEK"
                ]]
        
        self.UniverseSettings.Resolution = Resolution.Daily
        self.SetUniverseSelection(ManualUniverseSelectionModel(symbols))
        
        self.AddAlpha(InterestReleaseAlphaModel(self))
        
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel()) 
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())

    def OnOrderEvent(self, orderEvent):
        order = self.Transactions.GetOrderById(orderEvent.OrderId)
        self.Debug(f"{self.Time}: {order.Type}: {orderEvent}")


class InterestReleaseAlphaModel(AlphaModel):
    '''Alpha model that uses the Interest rate released by Fed to create insights'''
    
    def __init__(self, algorithm, period = 30, resolution = Resolution.Daily):
        '''
        Initializes a new instance of the InterestReleaseAlphaModel class
        Args:
            period: The prediction period
            resolution: The data resolution
        '''
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(resolution), period)
        self.pairs = []         # forex universe
        self.calendar = algorithm.AddData(TradingEconomicsCalendar, TradingEconomics.Calendar.UnitedStates.InterestRate).Symbol

        resolutionString = Extensions.GetEnumString(resolution, Resolution)
        self.Name = f'{self.__class__.__name__}({period},{resolutionString})'
    
    def Update(self, algorithm, data):
        '''
        Updates this alpha model with the latest data from the algorithm.
        This is called each time the algorithm receives data for subscribed securities
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated
        '''
        if not data.ContainsKey(self.calendar):
            return []
        
        insights = []
        
        fore_IR = data[self.calendar].Forecast      # Forecast Interest Rate
        prev_IR = data[self.calendar].Previous      # Previous released actual Interest Rate
        usdValueUp = fore_IR >= prev_IR
            
        for pair in self.pairs:
            
            direction = InsightDirection.Down
            if (pair.Value.startswith("USD") and usdValueUp) or (pair.Value.endswith("USD") and not usdValueUp):
                direction = InsightDirection.Up
            
            insights.append(Insight.Price(pair, self.predictionInterval, direction))

        return insights
        
        
    def OnSecuritiesChanged(self, algorithm, changes):
        '''
        Event fired each time the we add securities from the data feed
        
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm
        '''
        self.pairs = [ x.Symbol for x in changes.AddedSecurities ]