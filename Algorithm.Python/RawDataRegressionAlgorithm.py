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
from QuantConnect.Data.Auxiliary import FactorFile
from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Orders import OrderStatus
from QuantConnect.Orders.Fees import ConstantFeeModel

_ticker = "GOOGL";
_factorFile = FactorFile.Read(_ticker, Market.USA);
_expectedRawPrices = [ 1158.1100, 1158.7200,
1131.7800, 1114.2800, 1119.6100, 1114.5500, 1135.3200, 567.59000, 571.4900, 545.3000, 540.6400 ]

# <summary>
# In this algorithm we demonstrate how to use the raw data for our securities
# and verify that the behavior is correct.
# </summary>
# <meta name="tag" content="using data" />
# <meta name="tag" content="regression test" />
class RawDataRegressionAlgorithm(QCAlgorithm):

        def Initialize(self):
            self.SetStartDate(2014, 3, 25);    
            self.SetEndDate(2014, 4, 7);         
            self.SetCash(100000);                            

            # Set our DataNormalizationMode to raw
            self.UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;
            self._googl = self.AddEquity(_ticker, Resolution.Daily).Symbol;
        

        def OnData(self, data):
            if not self.Portfolio.Invested:
                self.SetHoldings(self._googl, 1);

            if (data.Bars.ContainsKey(self._googl)):
                googlData = data.Bars[self._googl];

                # Assert our volume matches what we expected
                if _expectedRawPrices.pop(0) != googlData.Close:
                    # Our values don't match lets try and give a reason why
                    dayFactor = _factorFile.GetPriceScaleFactor(googlData.Time);
                    probableRawPrice = googlData.Close / dayFactor; # Undo adjustment

                    if _expectedRawPrices.Current == probableRawPrice:
                        raise Exception("Close price was incorrect; it appears to be the adjusted value")
                    else:
                        raise Exception("Close price was incorrect; Data may have changed.")
            
