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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Indicators")

from QuantConnect import *
from QuantConnect.Python import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Data.Consolidators import *
from QuantConnect.Indicators import *
from System import *
from datetime import *

class CustomConsolidatorRegressionAlgorithm(QCAlgorithm):
    '''Custom Consolidator Regression Algorithm shows some examples of how to build custom 
    consolidators in Python.'''

    def Initialize(self):
        self.SetStartDate(2013,10,4)  
        self.SetEndDate(2013,10,11)    
        self.SetCash(100000)  
        self.AddEquity("SPY", Resolution.Minute)

        #Create 5 day QuoteBarConsolidator; set consolidated function; add to subscription manager
        fiveDayConsolidator = QuoteBarConsolidator(timedelta(days=5))
        fiveDayConsolidator.DataConsolidated += self.OnQuoteBarDataConsolidated
        self.SubscriptionManager.AddConsolidator("SPY", fiveDayConsolidator)

        #Create a 3:10PM custom quote bar consolidator
        timedConsolidator = DailyTimeQuoteBarConsolidator(time(hour=15, minute=10))
        timedConsolidator.DataConsolidated += self.OnQuoteBarDataConsolidated
        self.SubscriptionManager.AddConsolidator("SPY", timedConsolidator)

        #Create our entirely custom 2 day quote bar consolidator
        self.customConsolidator = CustomQuoteBarConsolidator(timedelta(days=2))
        self.customConsolidator.DataConsolidated += (self.OnQuoteBarDataConsolidated)
        self.SubscriptionManager.AddConsolidator("SPY", self.customConsolidator)

        #Create an indicator and register a consolidator to it
        self.movingAverage = SimpleMovingAverage(5)
        self.customConsolidator2 = CustomQuoteBarConsolidator(timedelta(hours=1))
        self.RegisterIndicator("SPY", self.movingAverage, self.customConsolidator2)


    def OnQuoteBarDataConsolidated(self, sender, bar):
        '''Function assigned to be triggered by consolidators.
        Designed to post debug messages to show how the examples work, including
        which consolidator is posting, as well as its values.

        If using an inherited class and not overwriting OnDataConsolidated
        we expect to see the super C# class as the sender type.

        Using sender.Period only works when all consolidators have a Period value.
        '''
        
        consolidatorInfo = str(type(sender)) + str(sender.Period)
       
        self.Debug("Bar Type: " + consolidatorInfo)
        self.Debug("Bar Range: " + bar.Time.ctime() + " - " + bar.EndTime.ctime())
        self.Debug("Bar value: " + str(bar.Close))
    
    def OnData(self, slice):
        test = slice.get_Values()

        if self.customConsolidator.Consolidated and slice.ContainsKey("SPY"):
            data = slice['SPY']
            
            if self.movingAverage.IsReady:
                if data.Value > self.movingAverage.Current.Price:
                    self.SetHoldings("SPY", .5)
                else :
                    self.SetHoldings("SPY", 0)
            


class DailyTimeQuoteBarConsolidator(QuoteBarConsolidator):
    '''A custom QuoteBar consolidator that inherits from C# class QuoteBarConsolidator. 

    This class shows an example of building on top of an existing consolidator class, it is important
    to note that this class can leverage the functions of QuoteBarConsolidator but its private fields
    (_period, _workingbar, etc.) are separate from this Python. For that reason if we want Scan() to work
    we must overwrite the function with our desired Scan function and trigger OnDataConsolidated().
    
    For this particular example we implemented the scan method to trigger a consolidated bar
    at closeTime everyday'''

    def __init__(self, closeTime):
        self.closeTime = closeTime
        self.workingBar = None
    
    def Update(self, data):
        '''Updates this consolidator with the specified data'''

        #If we don't have bar yet, create one
        if self.workingBar is None:
            self.workingBar = QuoteBar(data.Time,data.Symbol,data.Bid,data.LastBidSize,
                data.Ask,data.LastAskSize)

        #Update bar using QuoteBarConsolidator's AggregateBar()
        self.AggregateBar(self.workingBar, data)
        

    def Scan(self, time):
        '''Scans this consolidator to see if it should emit a bar due yet'''

        #If its our desired bar end time take the steps to 
        if time.hour == self.closeTime.hour and time.minute == self.closeTime.minute:

            #Set end time
            self.workingBar.EndTime = time

            #Emit event using QuoteBarConsolidator's OnDataConsolidated()
            self.OnDataConsolidated(self.workingBar)

            #Reset the working bar to None
            self.workingBar = None

class CustomQuoteBarConsolidator(PythonConsolidator):
    '''A custom quote bar consolidator that inherits from PythonConsolidator and implements 
    the IDataConsolidator interface, it must implement all of IDataConsolidator. Reference 
    PythonConsolidator.cs and DataConsolidatorPythonWrapper.py for more information.

    This class shows how to implement a consolidator from scratch in Python, this gives us more
    freedom to determine the behavior of the consolidator but can't leverage any of the built in
    functions of an inherited class.
    
    For this example we implemented a Quotebar from scratch'''

    def __init__(self, period):

        #IDataConsolidator required vars for all consolidators
        self.Consolidated = None        #Most recently consolidated piece of data.
        self.WorkingData = None         #Data being currently consolidated
        self.InputType = QuoteBar       #The type consumed by this consolidator
        self.OutputType = QuoteBar      #The type produced by this consolidator

        #Consolidator Variables
        self.Period = period
    
    def Update(self, data):
        '''Updates this consolidator with the specified data'''
        
        #If we don't have bar yet, create one
        if self.WorkingData is None:
            self.WorkingData = QuoteBar(data.Time,data.Symbol,data.Bid,data.LastBidSize,
                data.Ask,data.LastAskSize,self.Period)

        #Update bar using QuoteBar's update()
        self.WorkingData.Update(data.Value, data.Bid.Close, data.Ask.Close, 0, 
            data.LastBidSize, data.LastAskSize)

    def Scan(self, time):
        '''Scans this consolidator to see if it should emit a bar due to time passing'''

        if self.Period is not None and self.WorkingData is not None:
            if time - self.WorkingData.Time >= self.Period:

                #Trigger the event handler with a copy of self and the data
                self.OnDataConsolidated(self, self.WorkingData)

                #Set the most recent consolidated piece of data and then clear the workingData
                self.Consolidated = self.WorkingData
                self.WorkingData = None