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
AddReference("System.Windows.Forms")

from System import *
from System.Collections import *
from QuantConnect import *
from QuantConnect.Algorithm import *
import numpy as np

class BasicCSharpIntegrationTemplateAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.SetStartDate(2013,10, 7)  #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        
        self.AddEquity("SPY", Resolution.Second)
        
    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

            ## Calculate value of sin(10) for both python and C#
            self.Debug(f'According to Python, the value of sin(10) is {np.sin(10)}')
            self.Debug(f'According to C#, the value of sin(10) is {Math.Sin(10)}')