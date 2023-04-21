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
### Basic Continuous Futures Template Algorithm with extended market hours
### </summary>
class BasicTemplateContinuousFutureWithExtendedMarketAlgorithm(QCAlgorithm):
    '''Basic template algorithm simply initializes the date range and cash'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013, 7, 1)
        self.SetEndDate(2014, 1, 1)

        self._continuousContract = self.AddFuture(Futures.Indices.SP500EMini,
                                                  dataNormalizationMode = DataNormalizationMode.BackwardsRatio,
                                                  dataMappingMode = DataMappingMode.LastTradingDay,
                                                  contractDepthOffset = 0,
                                                  extendedMarketHours = True)

        self._fast = self.SMA(self._continuousContract.Symbol, 4, Resolution.Daily)
        self._slow = self.SMA(self._continuousContract.Symbol, 10, Resolution.Daily)
        self._currentContract = None

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        for changedEvent in data.SymbolChangedEvents.Values:
            if changedEvent.Symbol == self._continuousContract.Symbol:
                self.Log(f"SymbolChanged event: {changedEvent}")

        if not self.IsMarketOpen(self._continuousContract.Symbol):
            return

        if not self.Portfolio.Invested:
            if self._fast.Current.Value > self._slow.Current.Value:
                self._currentContract = self.Securities[self._continuousContract.Mapped]
                self.Buy(self._currentContract.Symbol, 1)
        elif self._fast.Current.Value < self._slow.Current.Value:
            self.Liquidate()

        if self._currentContract is not None and self._currentContract.Symbol != self._continuousContract.Mapped:
            self.Log(f"{Time} - rolling position from {self._currentContract.Symbol} to {self._continuousContract.Mapped}")

            currentPositionSize = self._currentContract.Holdings.Quantity
            self.Liquidate(self._currentContract.Symbol)
            self.Buy(self._continuousContract.Mapped, currentPositionSize)
            self._currentContract = self.Securities[self._continuousContract.Mapped]

    def OnOrderEvent(self, orderEvent):
        self.Debug("Purchased Stock: {0}".format(orderEvent.Symbol))

    def OnSecuritiesChanged(self, changes):
        self.Debug(f"{self.Time}-{changes}")
