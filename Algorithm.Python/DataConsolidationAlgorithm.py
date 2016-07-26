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
from QuantConnect.Data.Market import *
from QuantConnect.Data.Consolidators import *


class DataConsolidationAlgorithm(QCAlgorithm):
    '''Example algorithm giving an introduction into using IDataConsolidators.  
    
    This is an advanced QC concept and requires a certain level of comfort using C# and its event system.
     
    What is an IDataConsolidator?
    IDataConsolidator is a plugin point that can be used to transform your data more easily.
    In this example we show one of the simplest consolidators, the TradeBarConsolidator.
    This type is capable of taking a timespan to indicate how long each bar should be, or an
    integer to indicate how many bars should be aggregated into one.
     
    When a new 'consolidated' piece of data is produced by the IDataConsolidator,
    an event is fired with the argument of the new data.
     
    If you are unfamiliar with C# events, or events in general, you may find this useful. This is
    Microsoft's overview of events in C#
     
         http://msdn.microsoft.com/en-us/library/aa645739%28v=vs.71%29.aspx'''
    def __init__(self):
        self.__last = None


    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(DateTime(2013, 10, 07, 9, 30, 0))  #Set Start Date
        self.SetEndDate(self.StartDate.AddDays(1))           #Set End Date
        # Find more symbols here: http://quantconnect.com/data
        self.AddSecurity(SecurityType.Equity, "SPY")

        # define our 30 minute trade bar consolidator. we can
        # access the 30 minute bar from the DataConsolidated events
        thirtyMinuteConsolidator = TradeBarConsolidator(TimeSpan.FromMinutes(30))

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
        oneDayConsolidator = TradeBarConsolidator(TimeSpan.FromDays(1))

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


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        # we need to declare this method
        pass


    def OnEndOfDay(self, symbol):
        # close up shop each day and reset our 'last' value so we start tomorrow fresh
        self.Liquidate(symbol)
        self.__last = None


    def ThirtyMinuteBarHandler(self, sender, consolidated):
        '''This is our event handler for our 30 minute trade bar defined above in Initialize(). So each time the
        consolidator produces a new 30 minute bar, this function will be called automatically. The 'sender' parameter
         will be the instance of the IDataConsolidator that invoked the event, but you'll almost never need that!''' 

        if self.__last is not None and consolidated.Close > self.__last.Close:
            self.Log("{0} >> SPY >> LONG  >> 100 >> {1}".format(consolidated.Time.ToString("o"), self.Portfolio["SPY"].Quantity))
            self.Order("SPY", 100)

        elif self.__last is not None and consolidated.Close < self.__last.Close:
            self.Log("{0} >> SPY >> SHORT  >> 100 >> {1}".format(consolidated.Time.ToString("o"), self.Portfolio["SPY"].Quantity))
            self.Order("SPY", -100)

        self.__last = consolidated
        

    def ThreeDayBarConsolidatedHandler(self, sender, consolidated):
        ''' This is our event handler for our 3 day trade bar defined above in Initialize(). So each time the
        consolidator produces a new 3 day bar, this function will be called automatically. The 'sender' parameter
        will be the instance of the IDataConsolidator that invoked the event, but you'll almost never need that!'''       
        self.Log("{0} >> Plotting!".format(consolidated.Time.ToString("o")))
        self.Plot(consolidated.Symbol, "3HourBar", consolidated.Close)