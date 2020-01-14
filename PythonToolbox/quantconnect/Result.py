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

import pandas as pd
from math import isnan
from datetime import datetime

class Result:
    '''Result represents the live or backtest result of a successfully executed algorithm'''

    def __init__(self, json):
        '''Creates a new instance of Result'''
        tag = 'result'
        
        # LiveResults special case:
        self.LiveMode = 'LiveResults' in json
        if self.LiveMode:
            tag += 's'
            json = json.pop('LiveResults', json)

        result = json.pop(tag, json)

        self.Statistics = Information(result.pop('Statistics', {}))
        self.AlphaRuntimeStatistics  = Information(result.pop('AlphaRuntimeStatistics', {}))
        self.RuntimeStatistics = Information(result.pop('RuntimeStatistics', {}))
        self.ClosedTrades = self.__create_closed_trades_table(result)
        self.Charts = self.__create_charts_table(result)
        self.ProfitLoss = self.__create_profit_loss_table(result)
        self.Orders = self.__create_order_table(result)
        self.RollingWindow = self.__create_rolling_window_table(result)
        self.Information = Information(json)

    def __create_order_table(self, json):
        '''Creates a dataframe with the orders information'''
        orders = json.pop('Orders', None)
        if orders is None: return None

        # In Live results, orders is a list, so convert to dict keyed by Id.
        if isinstance(orders, list):
            orders = {x['Id']: x for x in orders}

        def __status_int_to_str(value):
            if value is None: return None
            values = [ 'New', 'Submitted', 'PartiallyFilled', 'Filled', 'Canceled', 'None', 'Invalid', 'CancelPending' ]
            return str(values) if value >= len(values) else values[value]

        def __security_type_int_to_str(value):
            if value is None: return None
            values = [ 'Base', 'Equity', 'Option', 'Commodity', 'Forex', 'Future', 'Cfd', 'Crypto' ]
            return str(values) if value >= len(values) else values[value]

        def __type_int_to_str(value):
            if value is None:   return None
            values = [ 'Market', 'Limit', 'StopMarket', 'StopLimit', 'MarketOnOpen', 'MarketOnClose', 'OptionExercise' ]
            return str(values) if value >= len(values) else values[value]

        columns = [
            'Id', 'Time', 'SecurityType', 'Symbol', 'PriceCurrency',
            'Quantity', 'Direction', 'Price', 'Type', 'Status', 'Tag',
            'LastFillTime', 'LastUpdateTime', 'CanceledTime' ]
        
        if self.LiveMode:
            columns += ['DeployId']

        drop_columns = [
            'BrokerId', 'ContingentId', 'CreatedTime', 'IsMarketable', 'Value',
            'AbsoluteQuantity', 'OrderSubmissionData', 'Properties', 'TimeInForce'] 

        df = pd.DataFrame([v for k, v in orders.items()], columns = columns + drop_columns)
        df = df.set_index('Id').drop(drop_columns, axis=1)
        df['Time'] = df['Time'].apply(self.__str_to_datetime)
        df['CanceledTime'] = df['CanceledTime'].apply(self.__str_to_datetime)
        df['LastFillTime'] = df['LastFillTime'].apply(self.__str_to_datetime)
        df['LastUpdateTime'] = df['LastUpdateTime'].apply(self.__str_to_datetime)
        df['Symbol'] = df['Symbol'].apply(lambda x: x['ID'])
        df['Type'] = df['Type'].apply(__type_int_to_str)
        df['Direction'] = df['Direction'].apply(self.__direction_int_to_str)
        df['Status'] = df['Status'].apply(__status_int_to_str)

        df['SecurityType'] = df['SecurityType'].apply(__security_type_int_to_str)
        return df.dropna(how='all', axis=1)

    def __create_profit_loss_table(self, json):
        '''Creates a dataframe with the algorithm P&L'''
        profitLoss = json.pop('ProfitLoss', None)
        if profitLoss is None: return None

        df = pd.DataFrame({'profit_loss' : profitLoss})
        df.index.name = 'time'
        df.index = df.index.map(self.__str_to_datetime)
        return df

    def __create_closed_trades_table(self, json):
        '''Creates a dataframe with the closed trades information'''
        total = json.get('TotalPerformance', None)
        if total is None: return None
        trades = total.get('ClosedTrades', None)
        if trades is None: return None

        df = pd.DataFrame(trades, columns = [
            'Symbol', 'Quantity', 'Direction', 'EntryTime', 'EntryPrice',
            'ExitPrice', 'ExitTime', 'Duration', 'EndTradeDrawdown', 
            'MAE', 'MFE', 'ProfitLoss', 'TotalFees'
            ])
        df['Symbol'] = df['Symbol'].apply(lambda x: x['ID'])
        df['Direction'] = df['Direction'].apply(self.__direction_int_to_str)
        df['EntryTime'] = df['EntryTime'].apply(self.__str_to_datetime)
        df['ExitTime'] = df['ExitTime'].apply(self.__str_to_datetime)
        df['Duration'] = df['ExitTime'] - df['EntryTime']
        return df.set_index('EntryTime')

    def __create_charts_table(self, json):
        '''Creates a dataframe with the charts information. 
        By converting the json into a dataframe, it makes data visualization easier'''
        charts = json.pop('Charts', None)
        if charts is None: return None

        df_charts = dict()
        for name, chart in charts.items():
            # Skip Meta data
            if name == 'Meta': continue
            columns = list()
            for column, series in chart['Series'].items():
                df = pd.DataFrame(series['Values'])
                df['x'] = pd.to_datetime(df['x'], unit='s')
                df = df.rename(index=str, columns={"x": "time", "y": column})
                columns.append(df.set_index('time'))
            if len(columns) > 1:
                df = pd.concat(columns, axis = 1, sort = True)
            df = df.fillna(method = 'ffill')
            df = df.fillna(method = 'bfill')
            df_charts[name] = df
        return df_charts

    def __create_rolling_window_table(self, json):
        '''Creates a dataframe with the rolling statistics information.
        By converting the json into a dataframe, it makes data visualization easier'''
        rollingWindow = json.pop('RollingWindow', None)
        if rollingWindow is None: return None

        series = dict()
        if 'TotalPerformance' in json:
            window = json['TotalPerformance']
            if window is None: window = dict()
            stats = window.get('PortfolioStatistics', dict())
            stats.update(window.get('TradeStatistics', dict()))
            series = {'TotalPerformance': pd.Series(stats)}

        for row, window in rollingWindow.items():
            stats = window.get('PortfolioStatistics', dict())
            stats.update(window.get('TradeStatistics', dict()))
            series.update({row: pd.Series(stats)})

        return pd.DataFrame(series).transpose()

    def __direction_int_to_str(self, value):
        if value is None: return None
        return [ 'Buy', 'Sell', 'Hold' ][value]

    def __str_to_datetime(self, value):
        if value is None: return None
        if isinstance(value, float) and isnan(value): return None
        fmt = '%Y-%m-%dT%H:%M:%SZ' if len(value) == 20 else '%Y-%m-%dT%H:%M:%S.%fZ'
        return datetime.strptime(value, fmt)


class Information(dict):
    def __init__(self, d):

        d = d if d is not None else {} 
        super().__init__(d)

        self.__repr = ''
        
        for k, b in d.items():
            a = k.replace(' ','').replace('-','')
            if isinstance(b, (list, tuple)):
                setattr(self, a, [Information(x) if isinstance(x, dict) else x for x in b])
            elif isinstance(b, dict):
                x = Information(b)
                setattr(self, a, x)
                s = '\n'.join([f'    {l}' for l in repr(x).splitlines()])
                self.__repr += f'{a}:\n{s}\n'
            else:
                setattr(self, a, b)
                self.__repr += f'{a}: {b}\n'

    def __repr__(self):
        return self.__repr