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
from time import sleep

### <summary>
### Example algorithm showing how to use QCAlgorithm.Train method
### </summary>
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="training" />
class TrainingExampleAlgorithm(QCAlgorithm):
    '''Example algorithm showing how to use QCAlgorithm.Train method'''

    def Initialize(self):

        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 14)

        self.AddEquity("SPY", Resolution.Daily)

        # Set TrainingMethod to be executed immediately
        self.Train(self.TrainingMethod)

        # Set TrainingMethod to be executed at 8:00 am every Sunday
        self.Train(self.DateRules.Every(DayOfWeek.Sunday), self.TimeRules.At(8 , 0), self.TrainingMethod)

    def TrainingMethod(self):

        self.Log(f'Start training at {self.Time}')
        # Use the historical data to train the machine learning model
        history = self.History(["SPY"], 200, Resolution.Daily)

        # ML code:
        pass