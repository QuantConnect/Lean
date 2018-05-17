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
AddReference("QuantConnect.Indicators")


from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Data.Custom import *
from QuantConnect.Algorithm import *
from NodaTime import DateTimeZone
from QuantConnect.Data.Custom import FxcmVolume
import decimal as d
import pandas as pd
from datetime import datetime

### <summary>
### Example demonstrating importing custom forex volume data to use with your algorithm from FXCM.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="indicator classes" />
### <meta name="tag" content="plotting indicators" />
### <meta name="tag" content="forex" />
class BasicTemplateFxcmVolumeAlgorithm(QCAlgorithm):

    def Initialize(self):
        
        self.SetCash(100000)               # Set Strategy Cash
        self.SetStartDate(2015, 2, 1)      # Set Start Date
        self.SetEndDate(2015, 3, 1)        # Set End Date
        
        # Add the forex asset
        # Find more symbols here: https://www.quantconnect.com/docs/data-library/forex
        self.syl = self.AddForex("EURUSD", Resolution.Minute, Market.FXCM).Symbol
        # Add the FXCM volume data
        self.vol_syl = self.AddData(FxcmVolume, "EURUSD_Vol", Resolution.Hour, TimeZones.Utc).Symbol
        self.price = self.Identity(self.syl, Resolution.Hour)
        self.volume = Identity("volIdentity")
        self.fastVWMA = IndicatorExtensions.WeightedBy(self.price, self.volume, 15)     
        self.slowVWMA = IndicatorExtensions.WeightedBy(self.price, self.volume, 300)
        # plot the difference between fastVWMA and slowVWMA
        self.PlotIndicator("VWMA", IndicatorExtensions.Minus(self.fastVWMA, self.slowVWMA))

    def OnData(self, data):
       
        if data.ContainsKey("EURUSD_VOL"): 
            self.volume.Update(IndicatorDataPoint(self.Time,data[self.vol_syl].Volume))

        if not self.slowVWMA.IsReady: return
    
        if (not self.Portfolio.Invested) or self.Portfolio[self.syl].IsShort:
            if self.fastVWMA.Current.Value > self.slowVWMA.Current.Value:
                self.SetHoldings(self.syl, 1)
                self.Log(str(self.Time) + " Take a Long Position.")

        elif self.fastVWMA.Current.Value < self.slowVWMA.Current.Value:
                self.SetHoldings(self.syl, -1)
                self.Log(str(self.Time) + " Take a Short Position.")