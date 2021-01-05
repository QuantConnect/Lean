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

from datetime import datetime, timedelta

MARKETS = ['empty', 'USA', 'FXCM', 'Oanda', 'Dukascopy', 'Bitfinex', 'Globex', 'NYMEX', 'CBOT', 'ICE', 'CBOE', 'NSE',
           'GDAX', 'Kraken', 'Bittrex', 'Bithumb', 'Binance', 'Poloniex', 'Coinone', 'HitBTC', 'OkCoin', 'Bitstamp']

SECURITY_TYPES = ['Base', 'Equity', 'Option', 'Commodity', 'Forex', 'Future', 'Cfd', 'Crypto']

OPTION_STYLES = ['American', 'European']

OPTION_RIGHTS = ['Call', 'Put']


class Symbol:
    def __init__(self, security_id):
        """
        Parses a Lean's SecurityIdentifier and decode its properties.

        The SecurityIdentifier contains information about a specific security, this includes the  symbol*, market,
        security type (equity, future, etc.) and other data specific to the SecurityType.

        * For equities, the SecurityIdentifier ticker is the first ticker symbol for which the security
        traded. This is the first date mentioned in the map_files.

        The Date property has different meaning fo different security types:
            - For equities this is the first date the security traded. Technically speaking, in LEAN, this is the first
            date mentioned in the map_files.
            - For options this is the expiry date. For futures this is the settlement date.
            - For forex and cfds this property will return None, as the field is not specified.

        :param security_id:  And string made of two components, the ticker and the unique SecurityIdentifier (sid),
        separated by a space.

        For securities with underlying, it can receive a pair of ticker-sid separated by an "|", the first represent
        the security itself, the second is its underlying's SecurityIdentifier.

        """
        security_type_width = 100
        security_type_offset = 1
        market_width = 1000
        market_offset = security_type_offset * security_type_width

        self.strike_default_scale = 4
        self.strike_default_scaleExpanded = 10 ** self.strike_default_scale
        self.strike_scale_width = 100
        self.strike_scale_offset = market_offset * market_width

        self.strike_width = 1000000
        self.strike_offset = self.strike_scale_offset * self.strike_scale_width

        option_style_width = 10
        option_style_offset = self.strike_offset * self.strike_width

        self.days_width = 100000
        self.days_offset = option_style_offset * option_style_width

        put_call_offset = self.days_offset * self.days_width
        put_call_width = 10

        self.ID = security_id
        is_option = False

        if '|' in security_id:
            # If contains '|' means this security has an underlying.
            [security_id, underlying_id] = security_id.split('|')
            self.Underlying = Symbol(underlying_id)
            is_option = True

        symbol, properties = self.parse_security_id(security_id)
        self.Symbol = symbol
        self.SecurityType = SECURITY_TYPES[self.extract_from_properties(properties,
                                                                        security_type_offset,
                                                                        security_type_width)]
        self.Market = MARKETS[self.extract_from_properties(properties,
                                                           market_offset,
                                                           market_width)]

        if self.SecurityType == 'Equity' or self.SecurityType == 'Option' or self.SecurityType == 'Future':
            self.Date = self.extract_date_from_properties(properties)
        else:
            self.Date = None

        if is_option:
            self.OptionRight = OPTION_RIGHTS[self.extract_from_properties(properties,
                                                                          put_call_offset,
                                                                          put_call_width)]
            self.OptionStyle = OPTION_STYLES[self.extract_from_properties(properties,
                                                                          option_style_offset,
                                                                          option_style_width)]
            self.StrikePrice = self.extract_strike_price_from_properties(properties)

    @staticmethod
    def extract_from_properties(properties, offset, width):
        """
        Generic method to extract securities properties from the decoded sid.
        """
        return (properties // offset) % width

    @staticmethod
    def decode_base_36(code):
        """
        Decode a string in base 36.
        :param code: string to decode
        :return: an integer representing the decoded sid.
        """
        base = 1
        result = 0
        ord_zero = ord('0')
        ord_a = ord('A')
        for char in code[::-1]:
            ord_char = ord(char)
            value = ord_char - ord_zero if ord_char <= 57 else ord_char - ord_a + 10
            result += base * value
            base *= 36
        return result

    def extract_date_from_properties(self, properties):
        """
        Extract the date from the decoded sid.

        :param properties: an integer representing the decoded sid.
        :return: a datetime object with the specific security Date.
        """
        days = (properties // self.days_offset) % self.days_width
        return datetime(1899, 12, 30, 0, 0, 0) + timedelta(days=float(days))

    def extract_strike_price_from_properties(self, properties):
        """
        Extract the date from the decoded sid.

        :param properties: an integer representing the decoded sid.
        :return: a float with the specific strike price.
        """
        scale = int((properties // self.strike_scale_offset) % self.strike_scale_width) - self.strike_default_scale
        unscaled_price = (properties // self.strike_offset) % self.strike_width
        return unscaled_price * 10 ** scale

    def parse_security_id(self, security_id):
        """
        Parses a single sid and return the ticker and the decoded sid.
        :param security_id: And string made of two components, the ticker and the unique SecurityIdentifier (sid),
        separated by a space.
        :return: a tuple of ticker and decoded sid
        """
        [symbol, code] = security_id.split(' ')
        properties = self.decode_base_36(code)
        return symbol, properties

    def __eq__(self, other):
        return self.ID == other.ID
