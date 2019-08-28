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
from QuantConnect.Python import PythonQuandl
from QuantConnect.Securities.Equity import EquityExchange
from datetime import datetime, timedelta

### <summary>
### This algorithm demonstrates the various ways you can call the History function,
### what it returns, and what you can do with the returned values.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="history and warm up" />
### <meta name="tag" content="history" />
### <meta name="tag" content="warm up" />
class HistoryAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.SetStartDate(2013,10, 8)  #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddEquity("SPY", Resolution.Daily)
        self.AddData(QuandlFuture,"CHRIS/CME_SP1", Resolution.Daily)
        # specifying the exchange will allow the history methods that accept a number of bars to return to work properly
        self.Securities["CHRIS/CME_SP1"].Exchange = EquityExchange()

        # we can get history in initialize to set up indicators and such
        self.spyDailySma = SimpleMovingAverage(14)

        # get the last calendar year's worth of SPY data at the configured resolution (daily)
        tradeBarHistory = self.History([self.Securities["SPY"].Symbol], timedelta(365))
        self.AssertHistoryCount("History<TradeBar>([\"SPY\"], timedelta(365))", tradeBarHistory, 250)

        # get the last calendar day's worth of SPY data at the specified resolution
        tradeBarHistory = self.History(["SPY"], timedelta(1), Resolution.Minute)
        self.AssertHistoryCount("History([\"SPY\"], timedelta(1), Resolution.Minute)", tradeBarHistory, 390)

        # get the last 14 bars of SPY at the configured resolution (daily)
        tradeBarHistory = self.History(["SPY"], 14)
        self.AssertHistoryCount("History([\"SPY\"], 14)", tradeBarHistory, 14)

        # get the last 14 minute bars of SPY
        tradeBarHistory = self.History(["SPY"], 14, Resolution.Minute)
        self.AssertHistoryCount("History([\"SPY\"], 14, Resolution.Minute)", tradeBarHistory, 14)

        # we can loop over the return value from these functions and we get TradeBars
        # we can use these TradeBars to initialize indicators or perform other math
        for index, tradeBar in tradeBarHistory.loc["SPY"].iterrows():
            self.spyDailySma.Update(index, tradeBar["close"])

        # get the last calendar year's worth of quandl data at the configured resolution (daily)
        quandlHistory = self.History(QuandlFuture, "CHRIS/CME_SP1", timedelta(365))
        self.AssertHistoryCount("History(QuandlFuture, \"CHRIS/CME_SP1\", timedelta(365))", quandlHistory, 250)

        # get the last 14 bars of SPY at the configured resolution (daily)
        quandlHistory = self.History(QuandlFuture, "CHRIS/CME_SP1", 14)
        self.AssertHistoryCount("History(QuandlFuture, \"CHRIS/CME_SP1\", 14)", quandlHistory, 14)

        # we can loop over the return values from these functions and we'll get Quandl data
        # this can be used in much the same way as the tradeBarHistory above
        self.spyDailySma.Reset()
        for index, quandl in quandlHistory.loc["CHRIS/CME_SP1"].iterrows():
            self.spyDailySma.Update(index, quandl["settle"])

        # get the last year's worth of all configured Quandl data at the configured resolution (daily)
        #allQuandlData = self.History(QuandlFuture, timedelta(365))
        #self.AssertHistoryCount("History(QuandlFuture, timedelta(365))", allQuandlData, 250)

        # get the last 14 bars worth of Quandl data for the specified symbols at the configured resolution (daily)
        allQuandlData = self.History(QuandlFuture, self.Securities.Keys, 14)
        self.AssertHistoryCount("History(QuandlFuture, self.Securities.Keys, 14)", allQuandlData, 14)

        # NOTE: using different resolutions require that they are properly implemented in your data type, since
        #  Quandl doesn't support minute data, this won't actually work, but if your custom data source has
        #  different resolutions, it would need to be implemented in the GetSource and Reader methods properly
        #quandlHistory = self.History(QuandlFuture, "CHRIS/CME_SP1", timedelta(7), Resolution.Minute)
        #quandlHistory = self.History(QuandlFuture, "CHRIS/CME_SP1", 14, Resolution.Minute)
        #allQuandlData = self.History(QuandlFuture, timedelta(365), Resolution.Minute)
        #allQuandlData = self.History(QuandlFuture, self.Securities.Keys, 14, Resolution.Minute)
        #allQuandlData = self.History(QuandlFuture, self.Securities.Keys, timedelta(1), Resolution.Minute)
        #allQuandlData = self.History(QuandlFuture, self.Securities.Keys, 14, Resolution.Minute)

        # get the last calendar year's worth of all quandl data
        allQuandlData = self.History(QuandlFuture, self.Securities.Keys, timedelta(365))
        self.AssertHistoryCount("History(QuandlFuture, self.Securities.Keys, timedelta(365))", allQuandlData, 250)

        # we can also access the return value from the multiple symbol functions to request a single
        # symbol and then loop over it
        singleSymbolQuandl = allQuandlData.loc["CHRIS/CME_SP1"]
        self.AssertHistoryCount("allQuandlData.loc[\"CHRIS/CME_SP1\"]", singleSymbolQuandl, 250)
        for  quandl in singleSymbolQuandl:
            # do something with 'CHRIS/CME_SP1.QuandlFuture' quandl data
            pass

        quandlSpyLows = allQuandlData.loc["CHRIS/CME_SP1"]["low"]
        self.AssertHistoryCount("allQuandlData.loc[\"CHRIS/CME_SP1\"][\"low\"]", quandlSpyLows, 250)
        for  low in quandlSpyLows:
            # do something with 'CHRIS/CME_SP1.QuandlFuture' quandl data
            pass


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

    def AssertHistoryCount(self, methodCall, tradeBarHistory, expected):
        count = len(tradeBarHistory.index)
        if count != expected:
            raise Exception("{} expected {}, but received {}".format(methodCall, expected, count))


class QuandlFuture(PythonQuandl):
    '''Custom quandl data type for setting customized value column name. Value column is used for the primary trading calculations and charting.'''
    def __init__(self):
        # Define ValueColumnName: cannot be None, Empty or non-existant column name
        # If ValueColumnName is "Close", do not use PythonQuandl, use Quandl:
        # self.AddData[QuandlFuture](self.crude, Resolution.Daily)
        self.ValueColumnName = "Settle"