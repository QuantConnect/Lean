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
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Data import *
from QuantConnect.Data.Custom.SEC import *
from QuantConnect.Data.UniverseSelection import *

class CustomDataAddDataCoarseSelectionRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(100000)

        self.UniverseSettings.Resolution = Resolution.Daily

        self.AddUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseSelector))

    def CoarseSelector(self, coarse):
        symbols = [i.Symbol for i in coarse if i.HasFundamentalData and i.DollarVolume > 500000000]

        self.customSymbols = []

        for symbol in symbols:
            self.customSymbols.append(self.AddData(SECReport8K, symbol, Resolution.Daily).Symbol)

        return symbols

    def OnData(self, data):
        for customSymbol in self.customSymbols:
            if not self.ActiveSecurities.ContainsKey(customSymbol.Underlying):
                raise Exception(f"Custom data undelrying ({customSymbol.Underlying}) Symbol was not found in active securities")
