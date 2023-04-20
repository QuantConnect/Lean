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
### Regression test illustrating how history from custom data sources can be requested. The <see cref="QCAlgorithm.History"/> method used in this
### example also allows to specify other parameters than just the resolution, such as the data normalization mode, the data mapping mode, etc.
### </summary>
class HistoryWithCustomDataSourceRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2014, 6, 5)
        self.SetEndDate(2014, 6, 6)

        self.aapl = self.AddData(CustomData, "AAPL", Resolution.Minute).Symbol
        self.spy = self.AddData(CustomData, "SPY", Resolution.Minute).Symbol

    def OnEndOfAlgorithm(self):
        aaplHistory = self.History(CustomData, self.aapl, self.StartDate, self.EndDate, Resolution.Minute,
            fillForward=False, extendedMarketHours=False, dataNormalizationMode=DataNormalizationMode.Raw).droplevel(0, axis=0)
        spyHistory = self.History(CustomData, self.spy, self.StartDate, self.EndDate, Resolution.Minute,
            fillForward=False, extendedMarketHours=False, dataNormalizationMode=DataNormalizationMode.Raw).droplevel(0, axis=0)

        if aaplHistory.size == 0 or spyHistory.size == 0:
            raise Exception("At least one of the history results is empty")

        # Check that both resutls contain the same data, since CustomData fetches APPL data regardless of the symbol
        if not aaplHistory.equals(spyHistory):
            raise Exception("Histories are not equal")

class CustomData(PythonData):
    '''Custom data source for the regression test algorithm, which returns AAPL equity data regardless of the symbol requested.'''

    def GetSource(self, config, date, isLiveMode):
        return TradeBar().GetSource(
            SubscriptionDataConfig(
                config,
                CustomData,
                # Create a new symbol as equity so we find the existing data files
                # Symbol.Create(config.MappedSymbol, SecurityType.Equity, config.Market)),
                Symbol.Create("AAPL", SecurityType.Equity, config.Market)),
            date,
            isLiveMode)

    def Reader(self, config, line, date, isLiveMode):
        tradeBar = TradeBar.ParseEquity(config, line, date)
        data = CustomData()
        data.Time = tradeBar.Time
        data.Value = tradeBar.Value
        data.Close = tradeBar.Close
        data.Open = tradeBar.Open
        data.High = tradeBar.High
        data.Low = tradeBar.Low
        data.Volume = tradeBar.Volume

        return data
