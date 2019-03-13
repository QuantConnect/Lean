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
AddReference("QuantConnect.Algorithm")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Data.UniverseSelection import *

### <summary>
### Demonstration of how to estimate constituents of QC500 index based on the company fundamentals
### The algorithm creates a default tradable and liquid universe containing 500 US equities
### which are chosen at the first trading day of each month.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="coarse universes" />
### <meta name="tag" content="fine universes" />
class ConstituentsQC500GeneratorAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetStartDate(2018, 1, 1)   # Set Start Date
        self.SetEndDate(2019, 1, 1)     # Set End Date
        self.SetCash(100000)            # Set Strategy Cash

        # Add QC500 Universe
        self.AddUniverse(self.Universe.Index.QC500)