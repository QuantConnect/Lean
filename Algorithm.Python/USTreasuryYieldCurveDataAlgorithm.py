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
from QuantConnect.Data.Custom.USTreasury import *

### <summary>
### Demonstration algorithm showing how to use and access U.S. Treasury yield curve data
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="yield curve" />
class USTreasuryYieldCurveDataAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2017, 1, 1)
        self.SetEndDate(2019, 6, 30)
        self.SetCash(100000)

        # Define the symbol and "type" of our generic data:
        self.symbol = self.AddData(USTreasuryYieldCurveRate, "USTYC").Symbol

    def OnData(self, slice):
        if not slice.ContainsKey(self.symbol):
            return

        curve = slice[self.symbol]
        self.Log(f"{self.Time} - 1M: {curve.OneMonth}, 2M: {curve.TwoMonth}, 3M: {curve.ThreeMonth}, 6M: {curve.SixMonth}, 1Y: {curve.OneYear}, 2Y: {curve.TwoYear}, 3Y: {curve.ThreeYear}, 5Y: {curve.FiveYear}, 10Y: {curve.TenYear}, 20Y: {curve.TwentyYear}, 30Y: {curve.ThirtyYear}")

