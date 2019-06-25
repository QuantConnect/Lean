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
from datetime import timedelta

### <summary>
### This example shows how we can execute a method that takes longer than Lean timeout limit
### This feature is useful for algorithms that train models
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class TrainExampleAlgorithm(QCAlgorithm):
    '''This example shows how we can execute a method that takes longer than Lean timeout limit
    This feature is useful for algorithms that train models'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.piString = ""

        self.SetStartDate(2013,10, 7)  #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date

        self.AddEquity("SPY")

        self.Schedule.On(
                self.DateRules.EveryDay("SPY"),
                self.TimeRules.AfterMarketOpen("SPY", 10),
                lambda : self.Train(lambda: self.CalculatePi(10000),
                                    timedelta(seconds=20),
                                    lambda: self.Debug(f"{self.Time} :: Pi: {self.piString}")
                                    )
                        )

    def CalculatePi(self, digits):
        # Credits to https://www.w3resource.com/projects/python/python-projects-1.php
        q, r, t, k, n, l = 1, 0, 1, 1, 3, 3

        decimal = digits
        counter = 0

        result = ""

        while counter != decimal + 1:
            if 4 * q + r - t < n * t:
                # yield digit
                result = result + str(n)
                # insert period after first digit
                if counter == 0:
                    result = result + '.'
                # end
                if decimal == counter:
                    break
                counter += 1
                nr = 10 * (r - n * t)
                n = ((10 * (3 * q + r)) // t) - 10 * n
                q *= 10
                r = nr
            else:
                nr = (2 * q + r) * l
                nn = (q * (7 * k) + 2 + (r * l)) // (t * l)
                q *= k
                t *= l
                l += 2
                k += 1
                n = nn
                r = nr

        self.piString = result
        return result


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''        
        if self.Portfolio.Invested: return
        self.SetHoldings("SPY", 1)