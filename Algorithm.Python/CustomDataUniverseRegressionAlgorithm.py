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

from datetime import datetime
from AlgorithmImports import *

### <summary>
### Custom data universe selection regression algorithm asserting it's behavior. See GH issue #6396
### </summary>
class CustomDataUniverseRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetStartDate(2014, 3, 24)
        self.SetEndDate(2014, 3, 31)

        self.UniverseSettings.Resolution = Resolution.Daily;
        self.AddUniverse(CoarseFundamental, "custom-data-universe", self.Selection)

        self._selectionTime = [datetime(2014, 3, 24), datetime(2014, 3, 25), datetime(2014, 3, 26),
                              datetime(2014, 3, 27), datetime(2014, 3, 28), datetime(2014, 3, 29), datetime(2014, 3, 30), datetime(2014, 3, 31)]

    def Selection(self, coarse):
        self.Debug(f"Universe selection called: {self.Time} Count: {len(coarse)}")

        expectedTime = self._selectionTime.pop(0)
        if expectedTime != self.Time:
            raise ValueError(f"Unexpected selection time {self.Time} expected {expectedTime}")

        # sort descending by daily dollar volume
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)

        # return the symbol objects of the top entries from our sorted collection
        underlyingSymbols = [ x.Symbol for x in sortedByDollarVolume[:10] ]
        customSymbols = []
        for symbol in underlyingSymbols:
            customSymbols.append(Symbol.CreateBase(MyPyCustomData, symbol))
        return underlyingSymbols + customSymbols

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            customData = data.Get(MyPyCustomData)
            symbols = [symbol for symbol in data.Keys if symbol.SecurityType is SecurityType.Equity]
            for symbol in symbols:
                self.SetHoldings(symbol, 1 / len(symbols))

                if len([x for x in customData.Keys if x.Underlying == symbol]) == 0:
                    raise ValueError(f"Custom data was not found for symbol {symbol}")

class MyPyCustomData(PythonData):

    def GetSource(self, config, date, isLiveMode):
        source = f"{Globals.DataFolder}/equity/usa/daily/{LeanData.GenerateZipFileName(config.Symbol, date, config.Resolution, config.TickType)}"
        return SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv)

    def Reader(self, config, line, date, isLiveMode):
        csv = line.split(',')
        _scaleFactor = 1 / 10000

        custom = MyPyCustomData()
        custom.Symbol = config.Symbol
        custom.Time =  datetime.strptime(csv[0], '%Y%m%d %H:%M')
        custom.Open = float(csv[1]) * _scaleFactor
        custom.High = float(csv[2]) * _scaleFactor
        custom.Low = float(csv[3]) * _scaleFactor
        custom.Close = float(csv[4]) * _scaleFactor
        custom.Value = float(csv[4]) * _scaleFactor
        custom.Period = Time.OneDay
        custom.EndTime = custom.Time + custom.Period

        return custom
