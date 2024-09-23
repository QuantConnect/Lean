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

    def initialize(self):
        self.set_start_date(2013,10, 8)  #Set Start Date
        self.set_end_date(2013,10,11)    #Set End Date
        self.set_cash(100000)           #Set Strategy Cash

        # Find more symbols here: http://quantconnect.com/data
        self.add_equity("SPY", Resolution.DAILY)
        IBM = self.add_data(CustomDataEquity, "IBM", Resolution.DAILY)
        # specifying the exchange will allow the history methods that accept a number of bars to return to work properly
        IBM.Exchange = EquityExchange()

        # we can get history in initialize to set up indicators and such
        self.daily_sma = SimpleMovingAverage(14)

        # get the last calendar year's worth of SPY data at the configured resolution (daily)
        trade_bar_history = self.history([self.securities["SPY"].symbol], timedelta(365))
        self.assert_history_count("History<TradeBar>([\"SPY\"], timedelta(365))", trade_bar_history, 250)

        # get the last calendar day's worth of SPY data at the specified resolution
        trade_bar_history = self.history(["SPY"], timedelta(1), Resolution.MINUTE)
        self.assert_history_count("History([\"SPY\"], timedelta(1), Resolution.MINUTE)", trade_bar_history, 390)

        # get the last 14 bars of SPY at the configured resolution (daily)
        trade_bar_history = self.history(["SPY"], 14)
        self.assert_history_count("History([\"SPY\"], 14)", trade_bar_history, 14)

        # get the last 14 minute bars of SPY
        trade_bar_history = self.history(["SPY"], 14, Resolution.MINUTE)
        self.assert_history_count("History([\"SPY\"], 14, Resolution.MINUTE)", trade_bar_history, 14)

        # get the historical data from last current day to this current day in minute resolution
        # with Fill Forward and Extended Market options
        interval_bar_history = self.history(["SPY"], self.time - timedelta(1), self.time, Resolution.MINUTE, True, True)
        self.assert_history_count("History([\"SPY\"], self.time - timedelta(1), self.time, Resolution.MINUTE, True, True)", interval_bar_history, 960)

        # get the historical data from last current day to this current day in minute resolution
        # with Extended Market option
        interval_bar_history = self.history(["SPY"], self.time - timedelta(1), self.time, Resolution.MINUTE, False, True)
        self.assert_history_count("History([\"SPY\"], self.time - timedelta(1), self.time, Resolution.MINUTE, False, True)", interval_bar_history, 828)

        # get the historical data from last current day to this current day in minute resolution
        # with Fill Forward option
        interval_bar_history = self.history(["SPY"], self.time - timedelta(1), self.time, Resolution.MINUTE, True, False)
        self.assert_history_count("History([\"SPY\"], self.time - timedelta(1), self.time, Resolution.MINUTE, True, False)", interval_bar_history, 390)

        # get the historical data from last current day to this current day in minute resolution
        interval_bar_history = self.history(["SPY"], self.time - timedelta(1), self.time, Resolution.MINUTE, False, False)
        self.assert_history_count("History([\"SPY\"], self.time - timedelta(1), self.time, Resolution.MINUTE, False, False)", interval_bar_history, 390)

        # we can loop over the return value from these functions and we get TradeBars
        # we can use these TradeBars to initialize indicators or perform other math
        for index, trade_bar in trade_bar_history.loc["SPY"].iterrows():
            self.daily_sma.update(index, trade_bar["close"])

        # get the last calendar year's worth of custom_data data at the configured resolution (daily)
        custom_data_history = self.history(CustomDataEquity, "IBM", timedelta(365))
        self.assert_history_count("History(CustomDataEquity, \"IBM\", timedelta(365))", custom_data_history, 250)

        # get the last 10 bars of IBM at the configured resolution (daily)
        custom_data_history = self.history(CustomDataEquity, "IBM", 14)
        self.assert_history_count("History(CustomDataEquity, \"IBM\", 14)", custom_data_history, 14)

        # we can loop over the return values from these functions and we'll get Custom data
        # this can be used in much the same way as the trade_bar_history above
        self.daily_sma.reset()
        for index, custom_data in custom_data_history.loc["IBM"].iterrows():
            self.daily_sma.update(index, custom_data["value"])

        # get the last 10 bars worth of Custom data for the specified symbols at the configured resolution (daily)
        all_custom_data = self.history(CustomDataEquity, self.securities.keys(), 14)
        self.assert_history_count("History(CustomDataEquity, self.securities.keys(), 14)", all_custom_data, 14 * 2)

        # NOTE: Using different resolutions require that they are properly implemented in your data type. If your
        #  custom data source has different resolutions, it would need to be implemented in the GetSource and 
        #  Reader methods properly.
        #custom_data_history = self.history(CustomDataEquity, "IBM", timedelta(7), Resolution.MINUTE)
        #custom_data_history = self.history(CustomDataEquity, "IBM", 14, Resolution.MINUTE)
        #all_custom_data = self.history(CustomDataEquity, timedelta(365), Resolution.MINUTE)
        #all_custom_data = self.history(CustomDataEquity, self.securities.keys(), 14, Resolution.MINUTE)
        #all_custom_data = self.history(CustomDataEquity, self.securities.keys(), timedelta(1), Resolution.MINUTE)
        #all_custom_data = self.history(CustomDataEquity, self.securities.keys(), 14, Resolution.MINUTE)

        # get the last calendar year's worth of all custom_data data
        all_custom_data = self.history(CustomDataEquity, self.securities.keys(), timedelta(365))
        self.assert_history_count("History(CustomDataEquity, self.securities.keys(), timedelta(365))", all_custom_data, 250 * 2)

        # we can also access the return value from the multiple symbol functions to request a single
        # symbol and then loop over it
        single_symbol_custom = all_custom_data.loc["IBM"]
        self.assert_history_count("all_custom_data.loc[\"IBM\"]", single_symbol_custom, 250)
        for  custom_data in single_symbol_custom:
            # do something with 'IBM.custom_data_equity' custom_data data
            pass

        custom_data_spyvalues = all_custom_data.loc["IBM"]["value"]
        self.assert_history_count("all_custom_data.loc[\"IBM\"][\"value\"]", custom_data_spyvalues, 250)
        for value in custom_data_spyvalues:
            # do something with 'IBM.custom_data_equity' value data
            pass

    def on_data(self, data):
        '''on_data event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.portfolio.invested:
            self.set_holdings("SPY", 1)

    def assert_history_count(self, method_call, trade_bar_history, expected):
        count = len(trade_bar_history.index)
        if count != expected:
            raise Exception("{} expected {}, but received {}".format(method_call, expected, count))


class CustomDataEquity(PythonData):
    def get_source(self, config, date, is_live):
        zip_file_name = LeanData.generate_zip_file_name(config.Symbol, date, config.Resolution, config.TickType)
        source = Globals.DataFolder + "/equity/usa/daily/" + zip_file_name
        return SubscriptionDataSource(source)

    def reader(self, config, line, date, is_live):
        if line == None:
            return None

        custom_data = CustomDataEquity()
        custom_data.symbol = config.symbol

        csv = line.split(",")
        custom_data.time = datetime.strptime(csv[0], '%Y%m%d %H:%M')
        custom_data.end_time = custom_data.time + timedelta(days=1)
        custom_data.value = float(csv[1])
        return custom_data
