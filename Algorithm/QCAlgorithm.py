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
AddReference('QuantConnect.Algorithm')
AddReference('QuantConnect.Common')

from QuantConnect import *
from QuantConnect.Algorithm import *
from datetime import datetime, timedelta
import pandas as pd

def get_milliseconds(start, timestamp):
    hours = timestamp.hour
    minutes = timestamp.minute
    seconds = timestamp.second
    microseconds = timestamp.microsecond
    milliseconds = ((hours * 60 + minutes) * 60 + seconds) * 1000 + microseconds / 1000
    return milliseconds if milliseconds >= start else 86400000

class QCAlgorithm(QCPyAlgorithm):
    def History(self, *args, **kwargs):

        # Convert args into kwargs since the class methods expect a dictionary
        if len(args) > 0:
            index = 0
            item = args[index]

            if type(item).__name__ == 'CLR Metatype':
                kwargs['type'] = item
                index = 1

            item = args[index]
            if isinstance(item, str) or isinstance(item, Symbol) or isinstance(item, list):
                kwargs['symbols'] = item
                index = index + 1

            item = args[index]
            if isinstance(item, datetime):
                kwargs['start'] = item
            elif isinstance(item, timedelta):
                kwargs['span'] = item
            elif isinstance(item, int):
                kwargs['periods'] = item
            index = index + 1

            if len(args) == index + 1:
                item = args[index]
                if isinstance(item, datetime):
                    kwargs['end'] = item
                    index = index + 1

            if len(args) == index + 1:
                kwargs['resolution'] = args[index]

        # Live and custom data with be dealt at C# side 
        if self.LiveMode or 'type' in kwargs:
            return self.PandasDataFrameHistory(kwargs)

        requests = self.GetPandasHistoryRequests(kwargs)

        df_list = []

        for request in requests:
            if not request.IsValid:
                self.Error(f"QCAlgorithm.History: File does not exist: {request.Source}")
                continue

            df = pd.read_csv(request.Source, header = None, index_col = 0, names = request.Names)

            period = request.Period

            # Convert datetime to pandas DatetimeIndex
            # Subtract one microsecond in tick resolution case
            openHours = request.MarketOpenHours
            if period == timedelta(0):
                openHours[-1] = openHours[-1] - timedelta(microseconds = 1)
            openHours = pd.DatetimeIndex(openHours)

            price_scale_factor = request.PriceScaleFactor
            if price_scale_factor != 1.0:
                i = 4 if period > timedelta(0) else 1
                df.iloc[:, 0:i] = df.iloc[:, 0:i] * price_scale_factor

            if period >= timedelta(hours = 1):
                df.index = pd.to_datetime(df.index) + period
                df = df[df.index.isin(openHours)]
            else:
                # remove some data before converting the index into datetime
                lower_bound = get_milliseconds(0, openHours[0])
                upper_bound = get_milliseconds(1 + lower_bound, openHours[1])
                
                df = df[(lower_bound <= df.index) & (df.index <= upper_bound)]

                df.index = (request.Date + period + timedelta(microseconds = 1000 * i) for i in df.index)
                df = df[(openHours[0] <= df.index) & (df.index <= openHours[1])]

            df.index = pd.MultiIndex.from_product([[request.Symbol.Value], df.index], names=['symbol', 'time'])
            df_list.append(df)

        if len(df_list) == 0:
            return pd.DataFrame()
        else:
            return pd.concat(df_list)