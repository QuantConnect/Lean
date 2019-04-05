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

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Selection import *
from datetime import datetime, timedelta

### <summary>
### Regression algorithm for testing ScheduledUniverseSelectionModel scheduling functions.
### </summary>
class ScheduledUniverseSelectionModelRegressionAlgorithm(QCAlgorithm):
    '''Regression algorithm for testing ScheduledUniverseSelectionModel scheduling functions.'''

    def Initialize(self):

        self.UniverseSettings.Resolution = Resolution.Hour

        self.SetStartDate(2017, 1, 1)
        self.SetEndDate(2017, 2, 1)

        # selection will run on mon/tues/thurs at 00:00/06:00/12:00/18:00
        self.SetUniverseSelection(ScheduledUniverseSelectionModel(
            self.DateRules.Every(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday),
            self.TimeRules.Every(timedelta(hours = 12)),
            self.SelectSymbols
            ))

        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(1)))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

        # some days of the week have different behavior the first time -- less securities to remove
        self.seenDays = []

    def SelectSymbols(self, dateTime):

        symbols = []
        weekday = dateTime.weekday()

        if weekday == 0 or weekday == 1:
            symbols.append(Symbol.Create('SPY', SecurityType.Equity, Market.USA))
        elif weekday == 2:
            # given the date/time rules specified in Initialize, this symbol will never be selected (not invoked on wednesdays)
            symbols.append(Symbol.Create('AAPL', SecurityType.Equity, Market.USA))
        else:
            symbols.append(Symbol.Create('IBM', SecurityType.Equity, Market.USA))

        if weekday == 1 or weekday == 3:
            symbols.append(Symbol.Create('EURUSD', SecurityType.Forex, Market.FXCM))
        elif weekday == 4:
            # given the date/time rules specified in Initialize, this symbol will never be selected (every 6 hours never lands on hour==1)
            symbols.append(Symbol.Create('EURGBP', SecurityType.Forex, Market.FXCM))
        else:
            symbols.append(Symbol.Create('NZDUSD', SecurityType.Forex, Market.FXCM))

        return symbols

    def OnSecuritiesChanged(self, changes):
        self.Log("{}: {}".format(self.Time, changes))

        weekday = self.Time.weekday()

        if weekday == 0:
            self.ExpectAdditions(changes, 'SPY', 'NZDUSD')
            if weekday not in self.seenDays:
                self.seenDays.append(weekday)
                self.ExpectRemovals(changes, None)
            else:
                self.ExpectRemovals(changes, 'EURUSD', 'IBM')

        if weekday == 1:
            self.ExpectAdditions(changes, 'EURUSD')
            if weekday not in self.seenDays:
                self.seenDays.append(weekday)
                self.ExpectRemovals(changes, 'NZDUSD')
            else:
                self.ExpectRemovals(changes, 'NZDUSD')

        if weekday == 2 or weekday == 4:
            # selection function not invoked on wednesdays (2) or friday (4)
            self.ExpectAdditions(changes, None)
            self.ExpectRemovals(changes, None)

        if weekday == 3:
            self.ExpectAdditions(changes, "IBM")
            self.ExpectRemovals(changes, "SPY")


    def OnOrderEvent(self, orderEvent):
        self.Log("{}: {}".format(self.Time, orderEvent))

    def ExpectAdditions(self, changes, *tickers):
        if tickers is None and changes.AddedSecurities.Count > 0:
            raise Exception("{}: Expected no additions: {}".format(self.Time, self.Time.weekday()))

        for ticker in tickers:
            if ticker is not None and ticker not in [s.Symbol.Value for s in changes.AddedSecurities]:
                raise Exception("{}: Expected {} to be added: {}".format(self.Time, ticker, self.Time.weekday()))

    def ExpectRemovals(self, changes, *tickers):
        if tickers is None and changes.RemovedSecurities.Count > 0:
            raise Exception("{}: Expected no removals: {}".format(self.Time, self.Time.weekday()))

        for ticker in tickers:
            if ticker is not None and ticker not in [s.Symbol.Value for s in changes.RemovedSecurities]:
                raise Exception("{}: Expected {} to be removed: {}".format(self.Time, ticker, self.Time.weekday()))
