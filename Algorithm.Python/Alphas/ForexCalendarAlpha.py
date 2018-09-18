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

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from datetime import timedelta, datetime
from decimal import Decimal
from Portfolio.EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel
from Execution.ImmediateExecutionModel import ImmediateExecutionModel
from Risk.NullRiskManagementModel import NullRiskManagementModel
from QuantConnect.Data.Custom import DailyFx
from QCAlgorithm import QCAlgorithm

### <summary>
### This demonstration alpha reads the DailyFx calendar and provides insights based upon
### the news' outlook for the root currency's(USD) associated pairs
### </summary>
class ForexCalendarAlgorithm(QCAlgorithmFramework):

    def Initialize(self):

        self.SetStartDate(2015, 7, 12)
        self.SetEndDate(2018, 7, 27)
        self.SetCash(100000)

        symbols = [Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("EURGBP", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("EURAUD", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("EURCHF", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("EURJPY", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("EURCHF", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("USDJPY", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("USDCHF", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("USDCAD", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("AUDUSD", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("AUDJPY", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("GBPJPY", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("GBPUSD", SecurityType.Forex, Market.Oanda),
                   Symbol.Create("NZDUSD", SecurityType.Forex, Market.Oanda)]

        # Initializes the class that provides DailyFx News
        self.AddData(DailyFx, "DFX", Resolution.Minute, TimeZones.Utc)

        # Set Our Universe
        self.UniverseSettings.Resolution = Resolution.Minute
        self.SetUniverseSelection(ManualUniverseSelectionModel(symbols))

        # Set to use our FxCalendar Alpha Model
        self.SetAlpha(FxCalendarTrigger())

        # Default Models For Other Framework Settings
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())

class FxCalendarTrigger(AlphaModel):

    def __init__(self):
        self.Name = "FxCalendarTrigger"

    def Update(self, algorithm, data):
        insights = []
        period = TimeSpan.FromMinutes(5)
        magnitude = 0.0005

        # We will create our insights when we recieve news
        if data.ContainsKey("DFX"):
            calendar = data["DFX"]

            # Only act if this is important news.
            if calendar.Importance != FxDailyImportance.High: return insights
            if calendar.Meaning == 0: return insights

            # Create insights for all active currencies in our universe when country matches currency
            for symbol in algorithm.ActiveSecurities.Keys:

                # Only process Fx assets.
                if (symbol.SecurityType != SecurityType.Forex):
                    continue

                pair = algorithm.Securities[symbol.Value]
                direction = InsightDirection.Flat

                if pair.BaseCurrencySymbol == calendar.Currency.upper():
                    direction = InsightDirection.Up if calendar.Meaning == FxDailyMeaning.Better else InsightDirection.Down
                elif pair.QuoteCurrency.Symbol == calendar.Currency.upper():
                    direction = InsightDirection.Down if calendar.Meaning == FxDailyMeaning.Better else InsightDirection.Up

                if (direction != InsightDirection.Flat):
                    insights.append(Insight.Price(symbol, period, direction, magnitude))
        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
         pass