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

from AlgorithmImports import *
from QuantConnect.Data.Auxiliary import *
from QuantConnect.Lean.Engine.DataFeeds import DefaultDataProvider

_ticker = "GOOGL"
_expected_raw_prices = [ 1158.72,
1131.97, 1114.28, 1120.15, 1114.51, 1134.89, 1135.1, 571.50, 545.25, 540.63 ]

# <summary>
# In this algorithm we demonstrate how to use the raw data for our securities
# and verify that the behavior is correct.
# </summary>
# <meta name="tag" content="using data" />
# <meta name="tag" content="regression test" />
class RawDataRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2014, 3, 25)
        self.set_end_date(2014, 4, 7)
        self.set_cash(100000)

        # Set our DataNormalizationMode to raw
        self.universe_settings.data_normalization_mode = DataNormalizationMode.RAW
        self._googl = self.add_equity(_ticker, Resolution.DAILY).symbol

        # Get our factor file for this regression
        data_provider = DefaultDataProvider()
        map_file_provider = LocalDiskMapFileProvider()
        map_file_provider.initialize(data_provider)
        factor_file_provider = LocalDiskFactorFileProvider()
        factor_file_provider.initialize(map_file_provider, data_provider)

        # Get our factor file for this regression
        self._factor_file = factor_file_provider.get(self._googl)


    def on_data(self, data):
        if not self.portfolio.invested:
            self.set_holdings(self._googl, 1)

        if data.bars.contains_key(self._googl):
            googl_data = data.bars[self._googl]

            # Assert our volume matches what we expected
            expected_raw_price = _expected_raw_prices.pop(0)
            if expected_raw_price != googl_data.close:
                # Our values don't match lets try and give a reason why
                day_factor = self._factor_file.get_price_scale_factor(googl_data.time)
                probable_raw_price = googl_data.close / day_factor  # Undo adjustment

                raise Exception("Close price was incorrect; it appears to be the adjusted value"
                    if expected_raw_price == probable_raw_price else
                   "Close price was incorrect; Data may have changed.")
