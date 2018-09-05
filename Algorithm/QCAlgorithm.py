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


class QCAlgorithm(QCPyAlgorithm):
    def History(self, *args, **kwargs):

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
            df = pd.read_csv(request.Source, header = None, index_col = 0, names = request.Names)
            
            if request.PriceScaleFactor != 1.0:
                df.iloc[:, 0:4] = df.iloc[:, 0:4] * request.PriceScaleFactor

            if request.Period >= timedelta(hours = 1):
                df.index = pd.to_datetime(df.index) + request.Period
            else:
                df.index = (request.Date + request.Period + timedelta(microseconds = 1000 * i) for i in df.index)

            df = df[(request.StartTime < df.index) & (df.index <= request.EndTime)]
            df.index = pd.MultiIndex.from_product([[request.Symbol.Value], df.index], names=['symbol', 'time'])
            df_list.append(df)

        return pd.concat(df_list)