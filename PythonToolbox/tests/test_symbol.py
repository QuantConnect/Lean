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

import pytest
from datetime import datetime
from quantconnect.symbol import Symbol

# noinspection PyPep8
spot_price_securities_cases = (
    ( 'security_id',       'ticker',   'security_type', 'market', 'date_arg'),
    [('SPY R735QTJ8XC9X',  'SPY',      'Equity',        'USA',    [1998, 1, 2]),
     ('AAPL R735QTJ8XC9X', 'AAPL',     'Equity',        'USA',    [1998, 1, 2]),
     ('EURUSD 5O',         'EURUSD',   'Forex',         'FXCM',   None),
     ('USDJPY 8G',         'USDJPY',   'Forex',         'Oanda',  None),
     ('WTICOUSD 8I',       'WTICOUSD', 'Cfd',           'Oanda',  None),
     ('BTCUSD XJ',         'BTCUSD',   'Crypto',        'GDAX',   None),
     ('ED XKDEAL18BYP5',   'ED',       'Future',        'USA',    [2020, 12, 15]),
     ])

option_security_cases = (
    ( 'security_id',                        'ticker', 'security_type', 'market', 'date_arg',    'underlying_id',    'option_right', 'option_style', 'strike_price'),
    [('SPY 3033WWUF8MUH2|SPY R735QTJ8XC9X', 'SPY',    'Option',        'USA',    [2015, 9, 18], 'SPY R735QTJ8XC9X', 'Put',          'European',     195.5)])


@pytest.mark.parametrize(*spot_price_securities_cases)
def test_spot_price_securities(security_id, ticker, security_type, market, date_arg):
    symbol = Symbol(security_id)
    assert symbol.ID == security_id
    assert symbol.Symbol == ticker
    assert symbol.SecurityType == security_type
    assert symbol.Market == market
    if symbol.SecurityType == 'Equity' or symbol.SecurityType == 'Future':
        assert symbol.Date == datetime(*date_arg)
    else:
        assert symbol.Date == None

@pytest.mark.parametrize(*option_security_cases)
def test_option_securities(security_id, ticker, security_type, market, date_arg, underlying_id,
                           option_right, option_style, strike_price):
    symbol = Symbol(security_id)
    assert symbol.ID == security_id
    assert symbol.Symbol == ticker
    assert symbol.SecurityType == security_type
    assert symbol.Market == market
    assert symbol.Date == datetime(*date_arg)
    assert symbol.Underlying == Symbol(underlying_id)
    assert symbol.OptionRight == option_right
    assert symbol.StrikePrice == strike_price
    assert symbol.OptionStyle == option_style


def test_equal_symbols_are_equal():
    assert Symbol('SPY R735QTJ8XC9X') == Symbol('SPY R735QTJ8XC9X')
