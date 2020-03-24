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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Securities import *
from QuantConnect.Data.Market import *
from QuantConnect.Data.Consolidators import *
from datetime import timedelta
from CustomDataRegressionAlgorithm import Bitcoin

### <summary>
### Regression algorithm reproducing data type bugs in the RegisterIndicator API. Related to GH 4205.
### </summary>
class RegisterIndicatorRegressionAlgorithm(QCAlgorithm):
    # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    def Initialize(self):
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2013, 10, 9)

        SP500 = Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.USA)
        self._symbol = _symbol = self.FutureChainProvider.GetFutureContractList(SP500, self.StartDate)[0]
        self.AddFutureContract(_symbol)
        self._indicators = []
        self._selectorCalled = [ False, False, False, False, False, False ]

        # QuoteBars
        indicator = CustomIndicator()
        consolidator = self.ResolveConsolidator(_symbol, Resolution.Minute, QuoteBar)
        self.RegisterIndicator(_symbol, indicator, consolidator)
        self._indicators.append(indicator)

        indicator2 = CustomIndicator()
        consolidator = self.ResolveConsolidator(_symbol, timedelta(minutes=1), QuoteBar)
        self.RegisterIndicator(_symbol, indicator2, consolidator, lambda bar: self.SetSelectorCalled(0) and bar)
        self._indicators.append(indicator2);

        indicator3 = SimpleMovingAverage(10)
        consolidator = self.ResolveConsolidator(_symbol, timedelta(minutes=1), QuoteBar)
        self.RegisterIndicator(_symbol, indicator3, consolidator, lambda bar: self.SetSelectorCalled(1) and (bar.Ask.High - bar.Bid.Low))
        self._indicators.append(indicator3);

        # TradeBar - default type
        movingAverage = SimpleMovingAverage(10)
        self.RegisterIndicator(_symbol, movingAverage, Resolution.Minute, lambda bar: self.SetSelectorCalled(2) and bar.Volume)
        self._indicators.append(movingAverage)

        movingAverage2 = SimpleMovingAverage(10);
        self.RegisterIndicator(_symbol, movingAverage2, Resolution.Minute)
        self._indicators.append(movingAverage2)

        movingAverage3 = SimpleMovingAverage(10)
        self.RegisterIndicator(_symbol, movingAverage3, timedelta(minutes=1))
        self._indicators.append(movingAverage3)

        movingAverage4 = SimpleMovingAverage(10)
        self.RegisterIndicator(_symbol, movingAverage4, timedelta(minutes=1), lambda bar: self.SetSelectorCalled(3) and bar.Volume)
        self._indicators.append(movingAverage4)

        # Custom data
        smaCustomData = SimpleMovingAverage(1)
        symbolCustom = self.AddData(Bitcoin, "BTC", Resolution.Minute).Symbol
        self.RegisterIndicator(symbolCustom, smaCustomData, timedelta(minutes=1), lambda bar: self.SetSelectorCalled(4) and bar.Volume)
        self._indicators.append(smaCustomData)

        smaCustomData2 = SimpleMovingAverage(1)
        self.RegisterIndicator(symbolCustom, smaCustomData2, Resolution.Minute)
        self._indicators.append(smaCustomData2)

        smaCustomData3 = SimpleMovingAverage(1)
        consolidator = self.ResolveConsolidator(symbolCustom, timedelta(minutes=1))
        self.RegisterIndicator(symbolCustom, smaCustomData3, consolidator, lambda bar: self.SetSelectorCalled(5) and bar.Volume)
        self._indicators.append(smaCustomData3);

    def SetSelectorCalled(self, position):
        self._selectorCalled[position] = True
        return True

    # OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
    def OnData(self, data):
        if not self.Portfolio.Invested:
           self.SetHoldings(self._symbol, 0.5)

    def OnEndOfAlgorithm(self):
        if any(not wasCalled for wasCalled in self._selectorCalled):
            raise ValueError("All selectors should of been called")
        if any(not indicator.IsReady for indicator in self._indicators):
            raise ValueError("All indicators should be ready")
        self.Log(f'Total of {len(self._indicators)} are ready')

class CustomIndicator(PythonIndicator):
    def __init__(self):
        self.Name = "Jose"
        self.Value = 0

    def Update(self, input):
        self.Value = input.Ask.High
        return True;
