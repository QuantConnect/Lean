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
AddReference("System.Core")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data.Custom import DailyFx
from QCAlgorithm import QCAlgorithm
import numpy as np

### <summary>
### Use event/fundamental calendar information (DailyFx) to design event based forex algorithms.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="forex" />
### <meta name="tag" content="dailyfx" />
class DailyFxAlgorithm(QCAlgorithm):
    ''' Add the Daily FX type to our algorithm and use its events. '''

    def Initialize(self):
        # Set the cash we'd like to use for our backtest
        self.SetCash(100000)
        # Set the start and the end date
        self.SetStartDate(2016,5,26)
        self.SetEndDate(2016,5,27)
        self._sliceCount = 0
        self._eventCount = 0
        self.AddData(DailyFx, "DFX", Resolution.Second, TimeZones.Utc)

    def OnData(self, data):
        # Daily Fx demonstration to call on
        result = data["DFX"]
        self._sliceCount +=1
        self.Debug("ONDATA >> {0} : {1}".format(self._sliceCount, result))