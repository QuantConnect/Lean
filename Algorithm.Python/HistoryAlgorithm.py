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
        self.AddData(CustomDataEquity, "IBM", Resolution.Daily)
        # specifying the exchange will allow the history methods that accept a number of bars to return to work properly

        # we can get history in initialize to set up indicators and such
        self.dailySma = SimpleMovingAverage(14)

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

        # get the historical data from last current day to this current day in minute resolution
        # with Fill Forward and Extended Market options
        intervalBarHistory = self.History(["SPY"], self.Time - timedelta(1), self.Time, Resolution.Minute, True, True)
        self.AssertHistoryCount("History([\"SPY\"], self.Time - timedelta(1), self.Time, Resolution.Minute, True, True)", intervalBarHistory, 960)

        # get the historical data from last current day to this current day in minute resolution
        # with Extended Market option
        intervalBarHistory = self.History(["SPY"], self.Time - timedelta(1), self.Time, Resolution.Minute, False, True)
        self.AssertHistoryCount("History([\"SPY\"], self.Time - timedelta(1), self.Time, Resolution.Minute, False, True)", intervalBarHistory, 828)

        # get the historical data from last current day to this current day in minute resolution
        # with Fill Forward option
        intervalBarHistory = self.History(["SPY"], self.Time - timedelta(1), self.Time, Resolution.Minute, True, False)
        self.AssertHistoryCount("History([\"SPY\"], self.Time - timedelta(1), self.Time, Resolution.Minute, True, False)", intervalBarHistory, 390)

        # get the historical data from last current day to this current day in minute resolution
        intervalBarHistory = self.History(["SPY"], self.Time - timedelta(1), self.Time, Resolution.Minute, False, False)
        self.AssertHistoryCount("History([\"SPY\"], self.Time - timedelta(1), self.Time, Resolution.Minute, False, False)", intervalBarHistory, 390)

        # we can loop over the return value from these functions and we get TradeBars
        # we can use these TradeBars to initialize indicators or perform other math
        for index, tradeBar in tradeBarHistory.loc["SPY"].iterrows():
            self.dailySma.Update(index, tradeBar["close"])

        # get the last calendar year's worth of customData data at the configured resolution (daily)
        customDataHistory = self.History(CustomDataEquity, "IBM", timedelta(365))
        self.AssertHistoryCount("History(CustomDataEquity, \"IBM\", timedelta(365))", customDataHistory, 10)

        # get the last 10 bars of IBM at the configured resolution (daily)
        customDataHistory = self.History(CustomDataEquity, "IBM", 14)
        self.AssertHistoryCount("History(CustomDataEquity, \"IBM\", 14)", customDataHistory, 10)

        # we can loop over the return values from these functions and we'll get Custom data
        # this can be used in much the same way as the tradeBarHistory above
        self.dailySma.Reset()
        for index, customData in customDataHistory.loc["IBM"].iterrows():
            self.dailySma.Update(index, customData["value"])

        # get the last 10 bars worth of Custom data for the specified symbols at the configured resolution (daily)
        allCustomData = self.History(CustomDataEquity, self.Securities.Keys, 14)
        self.AssertHistoryCount("History(CustomDataEquity, self.Securities.Keys, 14)", allCustomData, 20)

        # NOTE: Using different resolutions require that they are properly implemented in your data type. If your
        #  custom data source has different resolutions, it would need to be implemented in the GetSource and 
        #  Reader methods properly.
        #customDataHistory = self.History(CustomDataEquity, "IBM", timedelta(7), Resolution.Minute)
        #customDataHistory = self.History(CustomDataEquity, "IBM", 14, Resolution.Minute)
        #allCustomData = self.History(CustomDataEquity, timedelta(365), Resolution.Minute)
        #allCustomData = self.History(CustomDataEquity, self.Securities.Keys, 14, Resolution.Minute)
        #allCustomData = self.History(CustomDataEquity, self.Securities.Keys, timedelta(1), Resolution.Minute)
        #allCustomData = self.History(CustomDataEquity, self.Securities.Keys, 14, Resolution.Minute)

        # get the last calendar year's worth of all customData data
        allCustomData = self.History(CustomDataEquity, self.Securities.Keys, timedelta(365))
        self.AssertHistoryCount("History(CustomDataEquity, self.Securities.Keys, timedelta(365))", allCustomData, 20)

        # we can also access the return value from the multiple symbol functions to request a single
        # symbol and then loop over it
        singleSymbolCustom = allCustomData.loc["IBM"]
        self.AssertHistoryCount("allCustomData.loc[\"IBM\"]", singleSymbolCustom, 10)
        for  customData in singleSymbolCustom:
            # do something with 'IBM.CustomDataEquity' customData data
            pass

        customDataSpyValues = allCustomData.loc["IBM"]["value"]
        self.AssertHistoryCount("allCustomData.loc[\"IBM\"][\"value\"]", customDataSpyValues, 10)
        for value in customDataSpyValues:
            # do something with 'IBM.CustomDataEquity' value data
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


class CustomDataEquity(PythonData):
    def GetSource(self, config, date, isLive):
        source = "https://www.dl.dropboxusercontent.com/s/o6ili2svndzn556/custom_data.csv?dl=0"
        return SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile)

    def Reader(self, config, line, date, isLive):
        if line == None:
            return None

        customData = CustomDataEquity()
        customData.Symbol = config.Symbol

        csv = line.split(",")
        customData.Time = datetime.strptime(csv[0], '%Y%m%d %H:%M')
        customData.EndTime = customData.Time + timedelta(days=1)
        customData.Value = float(csv[1])
        return customData
