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
import numpy as np
import pandas as pd
from zipfile import ZipFile


class QCAlgorithm(QCPyAlgorithm):
    def History(self, *args, **kwargs):
        '''Executes the specified history request
        Args:
            Arguments must be presented in the following order or passed with a dictionary
            type: The data type of the symbols [optional]
            tickers: The symbols to retrieve historical data for [optional]

            periods [int]: The number of bars to request
            or
            span [timedelta]: The span over which to retrieve recent historical data
            or
            start [datetime]: The start time in the algorithm's time zone
            end [datetime]: The end time in the algorithm's time zone [optional]

            resolution: The resolution to request  [optional]
        Returns:
            pandas DataFrame with data satisfying the specified history request'''

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
                self.Error(f"QCAlgorithm.History: File does not exist: {request.ZipFilePath}")
                continue

            with ZipFile(request.ZipFilePath) as zip_file:
                try:
                    with zip_file.open(request.ZipEntryName) as buffer:
                        df = pd.read_csv(buffer, header = None, index_col = 0, names = request.Names)
                except KeyError:
                    self.Error(f"QCAlgorithm.History: File entry does not exist: {request.ZipEntryName}")
                    continue

            period = request.Period

            # Convert datetime to pandas DatetimeIndex
            # Subtract one microsecond in tick resolution case
            openHours = request.MarketOpenHours
            if period == timedelta(0):
                openHours = [x - timedelta(microseconds = 1) for x in openHours]
            openHours = pd.DatetimeIndex(openHours)

            if period >= timedelta(hours = 1):
                df.index = pd.to_datetime(df.index) + period
                df = df[df.index.isin(openHours)]
            else:
                date = request.Date + period
                df.index = (date + timedelta(microseconds = 1000 * i) for i in df.index)

                # only include market hours
                mask = []
                for i in range(0, len(openHours), 2):
                    if i == 0:
                        mask = (openHours[i] < df.index) & (df.index <= openHours[i+1])
                    else:
                        mask = (openHours[i] < df.index) & (df.index <= openHours[i+1]) | mask

                df = df[mask]

            # Do not include empty dataframe
            if df.empty: continue

            price_scale_factor = request.PriceScaleFactor
            if price_scale_factor != 1.0:
                ignore_factor_list = ['bidsize', 'asksize', 'quantity', 'volume']
                for name in df:
                    if name in ignore_factor_list:
                        continue
                    if np.issubdtype(df[name].dtype, np.number):
                        df[name] = df[name] * price_scale_factor

            df.index = pd.MultiIndex.from_product([[request.Symbol.Value], df.index], names=['symbol', 'time'])
            df_list.append(df)

        if len(df_list) == 0:
            return pd.DataFrame()

        # If we have data frames with different columns
        # we need to align them in axis 1
        column_count = set([len(df.columns) for df in df_list])
        if len(column_count) == 1:
            return pd.concat(df_list)

        return pd.concat(df_list, axis=1).groupby(level=0, axis=1).mean()