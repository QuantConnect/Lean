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

class VolumeShareSlippageModel:
    '''Represents a slippage model that is calculated by multiplying the price impact constant by the square of the ratio of the order to the total volume.'''
    
    def __init__(self, volume_limit: float = 0.025, price_impact: float = 0.1) -> None:
        '''Initializes a new instance of the "VolumeShareSlippageModel" class
        Args:
            volume_limit:
            price_impact: Defines how large of an impact the order will have on the price calculation'''
        self.volume_limit = volume_limit
        self.price_impact = price_impact

    def get_slippage_approximation(self, asset: Security, order: Order) -> float:
        '''Slippage Model. Return a decimal cash slippage approximation on the order.
        Args:
            asset: The Security instance of the security of the order.
            order: The Order instance being filled.'''
        last_data = asset.get_last_data()
        if not last_data:
           return 0

        bar_volume = 0
        slippage_percent = self.volume_limit * self.volume_limit * self.price_impact

        if last_data.data_type == MarketDataType.TRADE_BAR:
            bar_volume = last_data.volume
        elif last_data.data_type == MarketDataType.QUOTE_BAR:
            bar_volume = last_data.last_bid_size if order.direction == OrderDirection.BUY else last_data.last_ask_size
        else:
           raise InvalidOperationException(Messages.VolumeShareSlippageModel.InvalidMarketDataType(last_data))

        # If volume is zero or negative, we use the maximum slippage percentage since the impact of any quantity is infinite
        # In FX/CFD case, we issue a warning and return zero slippage
        if bar_volume <= 0:
            security_type = asset.symbol.id.security_type
            if security_type == SecurityType.CFD or security_type == SecurityType.FOREX or security_type == SecurityType.CRYPTO:
                Log.error(Messages.VolumeShareSlippageModel.VolumeNotReportedForMarketDataType(security_type))
                return 0

            Log.error(Messages.VolumeShareSlippageModel.NegativeOrZeroBarVolume(bar_volume, slippage_percent))
        else:
            # Ratio of the order to the total volume
            volume_share = min(order.absolute_quantity / bar_volume, self.volume_limit)

            slippage_percent = volume_share * volume_share * self.price_impact

        return slippage_percent * last_data.Value;
