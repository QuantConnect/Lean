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
from QuantConnect.Algorithm import *

### <summary>
### Example algorithm showing that Slice, Securities and Portfolio behave as a Python Dictionary
### </summary>
class PythonDictionaryFeatureRegressionAlgorithm(QCAlgorithm):
    '''Example algorithm showing that Slice, Securities and Portfolio behave as a Python Dictionary'''

    def Initialize(self):

        self.SetStartDate(2013,10, 7)  #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        self.spySymbol = self.AddEquity("SPY").Symbol
        self.ibmSymbol = self.AddEquity("IBM").Symbol
        self.aigSymbol = self.AddEquity("AIG").Symbol
        self.aaplSymbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA)

        dateRules = self.DateRules.On(2013, 10, 7)
        self.Schedule.On(dateRules, self.TimeRules.At(13, 0), self.TestSecuritiesDictionary)
        self.Schedule.On(dateRules, self.TimeRules.At(14, 0), self.TestPortfolioDictionary)
        self.Schedule.On(dateRules, self.TimeRules.At(15, 0), self.TestSliceDictionary)

    def TestSliceDictionary(self):
        slice = self.CurrentSlice

        symbols = ', '.join([f'{x}' for x in slice.keys()])
        sliceData = ', '.join([f'{x}' for x in slice.values()])
        sliceBars = ', '.join([f'{x}' for x in slice.Bars.values()])

        if self.spySymbol not in slice:
            raise Exception('SPY is not in Slice')

        spy = slice.get(self.spySymbol)
        if spy is None:
            raise Exception('SPY is not in Slice')

        for symbol, bar in slice.Bars.items():
            self.Plot(symbol, 'Price', bar.Close)


    def TestSecuritiesDictionary(self):
        symbols = ', '.join([f'{x}' for x in self.Securities.keys()])
        leverages = ', '.join([str(x.GetLastData()) for x in self.Securities.values()])

        if "IBM" not in self.Securities:
            raise Exception('IBM is not in Securities')

        ibm = self.Securities.get(self.ibmSymbol)
        if ibm is None:
            raise Exception('ibm is None')

        aapl = self.Securities.get(self.aaplSymbol)
        if aapl is not None:
            raise Exception('aapl is not None')

        for symbol, security in self.Securities.items():
            self.Plot(symbol, 'Price', security.Price)

    def TestPortfolioDictionary(self):
        symbols = ', '.join([f'{x}' for x in self.Portfolio.keys()])
        leverages = ', '.join([f'{x.Symbol}: {x.Leverage}' for x in self.Portfolio.values()])

        if "AIG" not in self.Securities:
            raise Exception('AIG is not in Portfolio')

        aig = self.Portfolio.get(self.aigSymbol)
        if aig is None:
            raise Exception('aig is None')

        aapl = self.Portfolio.get(self.aaplSymbol)
        if aapl is not None:
            raise Exception('aapl is not None')

        for symbol, holdings in self.Portfolio.items():
            msg = f'{symbol}: {holdings.Leverage}'

    def OnEndOfAlgorithm(self):

        portfolioCopy = self.Portfolio.copy()
        try:
            self.Portfolio.clear()  # Throws exception
        except Exception as e:
            self.Debug(e)

        bar = self.Securities.pop("SPY")
        length = len(self.Securities)
        if length != 2:
            raise Exception(f'After popping SPY, Securities should have 2 elements, {length} found')

        securitiesCopy = self.Securities.copy()
        self.Securities.clear()    # Does not throw


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1/3)
            self.SetHoldings("IBM", 1/3)
            self.SetHoldings("AIG", 1/3)