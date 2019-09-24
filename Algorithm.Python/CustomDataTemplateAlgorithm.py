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
from QuantConnect.Data import *
from QuantConnect.Data.Custom.SEC import *
from QuantConnect.Data.UniverseSelection import *

### <summary>
### Template algorithm combining coarse selection with custom data
### </summary>
class CustomTemplateAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2014,3,25)    #Set Start Date
        self.SetEndDate(2014,4,7)      #Set End Date
        self.SetCash(50000)            #Set Strategy Cash

        # what resolution should the data *added* to the universe be?
        self.UniverseSettings.Resolution = Resolution.Daily

        # this add universe method accepts a single parameter that is a function that
        # accepts an IEnumerable<CoarseFundamental> and returns IEnumerable<Symbol>
        self.AddUniverse(self.CoarseSelectionFunction)

        self.__numberOfSymbols = 3

    # sort the data by daily dollar volume and take the top 'NumberOfSymbols'
    def CoarseSelectionFunction(self, coarse):
        # sort descending by daily dollar volume
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)

        # return the symbol objects of the top entries from our sorted collection
        return [ x.Symbol for x in sortedByDollarVolume[:self.__numberOfSymbols] ]

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        # Trading logic could be place here
        pass

    def OnSecuritiesChanged(self, changes):
        '''Here we will add and remove our custom data types based on the securities being added by
            the coarse selection method.
            We want to make sure we remove our custom data types to avoid unnecessary resource consumption
        '''
        for added in [i for i in changes.AddedSecurities if i.Symbol.SecurityType == SecurityType.Equity]:
            self.AddData(SECReport8K, added.Symbol, Resolution.Daily)
            self.AddData(SECReport10K, added.Symbol, Resolution.Daily)

        for removed in [i for i in changes.RemovedSecurities if i.Symbol.SecurityType == SecurityType.Equity]:
            self.RemoveCustomSecurities(removed.Symbol, SECReport8K, SECReport10K)