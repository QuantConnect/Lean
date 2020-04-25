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
from QuantConnect.Securities.Option import OptionPriceModels

### <summary>
### Example demonstrating how to define an option price model.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="options" />
### <meta name="tag" content="filter selection" />
### <meta name="tag" content="option price model" />
class BasicTemplateOptionsPriceModel(QCAlgorithm):
    '''Example demonstrating how to define an option price model.'''

    def Initialize(self):
        self.SetStartDate(2020, 1, 1)
        self.SetEndDate(2020, 1, 5)
        self.SetCash(100000)

        # Add the option
        option = self.AddOption("AAPL")
        self.optionSymbol = option.Symbol

        # Add the initial contract filter
        option.SetFilter(-3, +3, 0, 31)

        # Define the Option Price Model
        option.PriceModel = OptionPriceModels.CrankNicolsonFD()
        #option.PriceModel = OptionPriceModels.BlackScholes()
        #option.PriceModel = OptionPriceModels.AdditiveEquiprobabilities()
        #option.PriceModel = OptionPriceModels.BaroneAdesiWhaley()
        #option.PriceModel = OptionPriceModels.BinomialCoxRossRubinstein()
        #option.PriceModel = OptionPriceModels.BinomialJarrowRudd()
        #option.PriceModel = OptionPriceModels.BinomialJoshi()
        #option.PriceModel = OptionPriceModels.BinomialLeisenReimer()
        #option.PriceModel = OptionPriceModels.BinomialTian()
        #option.PriceModel = OptionPriceModels.BinomialTrigeorgis()
        #option.PriceModel = OptionPriceModels.BjerksundStensland()
        #option.PriceModel = OptionPriceModels.Integral()

        # Set warm up with 30 trading days to warm up the underlying volatility model
        self.SetWarmUp(30, Resolution.Daily)


    def OnData(self,slice):
        '''OnData will test whether the option contracts has a non-zero Greeks.Delta'''

        if self.IsWarmingUp or not slice.OptionChains.ContainsKey(self.optionSymbol):
            return

        chain = slice.OptionChains[self.optionSymbol]
        if not any([x for x in chain if x.Greeks.Delta != 0]):
            self.Log(f'No contract with Delta != 0')