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

import clr
clr.AddReference("System")
clr.AddReference("QuantConnect.Algorithm")
clr.AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Brokerages import *


class CustomBrokerageErrorHandlerAlgorithm(QCAlgorithm):
    '''QCU How do I handle brokerage messages in a custom way?

    Often you may want more stability and fault tolerance so you may want to control
    what happens with brokerage messages. Using the custom messaging handler you
    can ensure your algorithm continues operation through connection failures.'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2013,10,07)  #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddSecurity(SecurityType.Equity, "SPY")

        #Set the brokerage message handler:
        self.SetBrokerageMessageHandler(CustomBrokerageMessageHandler(self))

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if self.Portfolio.HoldStock:
            return
        
        self.Order("SPY", 100)
        self.Debug("Purchased SPY on {0}".format(self.Time.ToShortDateString()))

class CustomBrokerageMessageHandler(IBrokerageMessageHandler):
    '''Handle the error messages in a custom manner'''
    
    def __init__(self, algo):
        self._algo = algo
        
    def Handle(self, message):
        '''Process the brokerage message event. Trigger any actions in the algorithm or notifications system required.
        
        Arguments:
            message: Message object
        '''
        toLog = "{0} Event: {1}".format(self._algo.Time.ToString("o"), message.Message)
        self._algo.Debug(toLog) 
        self._algo.Log(toLog)