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
from QuantConnect.Data.Consolidators import *
from datetime import timedelta

### <summary>
### Example algorithm giving an introduction into using IDataConsolidators.
### This is an advanced QC concept and requires a certain level of comfort using C# and its event system.
###
### What is an IDataConsolidator?
### IDataConsolidator is a plugin point that can be used to transform your data more easily.
### In this example we show one of the simplest consolidators, the TradeBarConsolidator.
### This type is capable of taking a timespan to indicate how long each bar should be, or an
### integer to indicate how many bars should be aggregated into one.
###
### When a new 'consolidated' piece of data is produced by the IDataConsolidator, an event is fired
### with the argument of the new data.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="consolidating data" />
class DataConsolidationAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(DateTime(2013, 10, 7, 9, 30, 0))  #Set Start Date
        self.SetEndDate(self.StartDate + timedelta(60))          #Set End Date
        # Find more symbols here: http://quantconnect.com/data
        self.AddEquity("SPY")
        self.AddForex("EURUSD", Resolution.Hour)

        # define our 30 minute trade bar consolidator. we can
        # access the 30 minute bar from the DataConsolidated events
        thirtyMinuteConsolidator = TradeBarConsolidator(timedelta(minutes=30))

        # attach our event handler. the event handler is a function that will
        # be called each time we produce a new consolidated piece of data.
        thirtyMinuteConsolidator.DataConsolidated += self.ThirtyMinuteBarHandler

        # this call adds our 30 minute consolidator to
        # the manager to receive updates from the engine
        self.SubscriptionManager.AddConsolidator("SPY", thirtyMinuteConsolidator)

        # here we'll define a slightly more complex consolidator. what we're trying to produce is
        # a 3 day bar. Now we could just use a single TradeBarConsolidator like above and pass in
        # TimeSpan.FromDays(3), but in reality that's not what we want. For time spans of longer than
        # a day we'll get incorrect results around weekends and such. What we really want are tradeable
        # days. So we'll create a daily consolidator, and then wrap it with a 3 count consolidator.

        # first define a one day trade bar -- this produces a consolidated piece of data after a day has passed
        oneDayConsolidator = TradeBarConsolidator(timedelta(1))

        # next define our 3 count trade bar -- this produces a consolidated piece of data after it sees 3 pieces of data
        threeCountConsolidator = TradeBarConsolidator(3)

        # here we combine them to make a new, 3 day trade bar. The SequentialConsolidator allows composition of
        # consolidators. It takes the consolidated output of one consolidator (in this case, the oneDayConsolidator)
        # and pipes it through to the threeCountConsolidator.  His output will be a 3 day bar.
        three_oneDayBar = SequentialConsolidator(oneDayConsolidator, threeCountConsolidator)

        # attach our handler
        three_oneDayBar.DataConsolidated += self.ThreeDayBarConsolidatedHandler

        # this call adds our 3 day to the manager to receive updates from the engine
        self.SubscriptionManager.AddConsolidator("SPY", three_oneDayBar)

        # Custom monthly consolidator
        customMonthlyConsolidator = TradeBarConsolidator(self.CustomMonthly)
        customMonthlyConsolidator.DataConsolidated += self.CustomMonthlyHandler
        self.SubscriptionManager.AddConsolidator("SPY", customMonthlyConsolidator)

        # API convenience method for easily receiving consolidated data
        self.Consolidate("SPY", timedelta(minutes=45), self.FortyFiveMinuteBarHandler)
        self.Consolidate("SPY", Resolution.Hour, self.HourBarHandler)
        self.Consolidate("EURUSD", Resolution.Daily, self.DailyEurUsdBarHandler)

        # API convenience method for easily receiving weekly-consolidated data
        self.Consolidate("SPY", CalendarType.Weekly, self.CalendarTradeBarHandler)
        self.Consolidate("EURUSD", CalendarType.Weekly, self.CalendarQuoteBarHandler)

        # API convenience method for easily receiving monthly-consolidated data
        self.Consolidate("SPY", CalendarType.Monthly, self.CalendarTradeBarHandler);
        self.Consolidate("EURUSD", CalendarType.Monthly, self.CalendarQuoteBarHandler);

        # some securities may have trade and quote data available, so we can choose it based on TickType:
        #self.Consolidate("BTCUSD", Resolution.Hour, TickType.Trade, self.HourBarHandler)   # to get TradeBar
        #self.Consolidate("BTCUSD", Resolution.Hour, TickType.Quote, self.HourBarHandler)   # to get QuoteBar (default)

        self.consolidatedHour = False
        self.consolidated45Minute = False
        self.__last = None

    def OnData(self, data):
        '''We need to declare this method'''
        pass


    def OnEndOfDay(self):
        # close up shop each day and reset our 'last' value so we start tomorrow fresh
        self.Liquidate("SPY")
        self.__last = None

    def ThirtyMinuteBarHandler(self, sender, consolidated):
        '''This is our event handler for our 30 minute trade bar defined above in Initialize(). So each time the
        consolidator produces a new 30 minute bar, this function will be called automatically. The 'sender' parameter
         will be the instance of the IDataConsolidator that invoked the event, but you'll almost never need that!'''

        if self.__last is not None and consolidated.Close > self.__last.Close:
            self.Log(f"{consolidated.Time} >> SPY >> LONG  >> 100 >> {self.Portfolio['SPY'].Quantity}")
            self.Order("SPY", 100)

        elif self.__last is not None and consolidated.Close < self.__last.Close:
            self.Log(f"{consolidated.Time} >> SPY >> SHORT  >> 100 >> {self.Portfolio['SPY'].Quantity}")
            self.Order("SPY", -100)

        self.__last = consolidated


    def ThreeDayBarConsolidatedHandler(self, sender, consolidated):
        ''' This is our event handler for our 3 day trade bar defined above in Initialize(). So each time the
        consolidator produces a new 3 day bar, this function will be called automatically. The 'sender' parameter
        will be the instance of the IDataConsolidator that invoked the event, but you'll almost never need that!'''
        self.Log(f"{consolidated.Time} >> Plotting!")
        self.Plot(consolidated.Symbol.Value, "3HourBar", consolidated.Close)

    def FortyFiveMinuteBarHandler(self, consolidated):
        ''' This is our event handler for our 45 minute consolidated defined using the Consolidate method'''
        self.consolidated45Minute = True
        self.Log(f"{consolidated.EndTime} >> FortyFiveMinuteBarHandler >> {consolidated.Close}")

    def HourBarHandler(self, consolidated):
        '''This is our event handler for our one hour consolidated defined using the Consolidate method'''
        self.consolidatedHour = True
        self.Log(f"{consolidated.EndTime} >> FortyFiveMinuteBarHandler >> {consolidated.Close}")

    def DailyEurUsdBarHandler(self, consolidated):
        '''This is our event handler for our daily consolidated defined using the Consolidate method'''
        self.Log(f"{consolidated.EndTime} EURUSD Daily consolidated.")

    def CalendarTradeBarHandler(self, tradeBar):
        self.Log(f'{self.Time} :: {tradeBar.Time} {tradeBar.Close}')

    def CalendarQuoteBarHandler(self, quoteBar):
        self.Log(f'{self.Time} :: {quoteBar.Time} {quoteBar.Close}')

    def CustomMonthly(self, dt):
        '''Custom Monthly Func'''
        start = dt.replace(day=1).date()
        end = dt.replace(day=28) + timedelta(4)
        end = (end - timedelta(end.day-1)).date()
        return CalendarInfo(start, end - start)

    def CustomMonthlyHandler(self, sender, consolidated):
        '''This is our event handler Custom Monthly function'''
        self.Log(f"{consolidated.Time} >> CustomMonthlyHandler >> {consolidated.Close}")

    def OnEndOfAlgorithm(self):
        if not self.consolidatedHour:
            raise Exception("Expected hourly consolidator to be fired.")

        if not self.consolidated45Minute: 
            raise Exception("Expected 45-minute consolidator to be fired.")