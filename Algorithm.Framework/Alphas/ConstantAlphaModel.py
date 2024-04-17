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

class ConstantAlphaModel(AlphaModel):
    ''' Provides an implementation of IAlphaModel that always returns the same insight for each security'''

    def __init__(self, type, direction, period, magnitude = None, confidence = None, weight = None):
        '''Initializes a new instance of the ConstantAlphaModel class
        Args:
            type: The type of insight
            direction: The direction of the insight
            period: The period over which the insight with come to fruition
            magnitude: The predicted change in magnitude as a +- percentage
            confidence: The confidence in the insight
            weight: The portfolio weight of the insights'''
        self.type = type
        self.direction = direction
        self.period = period
        self.magnitude = magnitude
        self.confidence = confidence
        self.weight = weight
        self.securities = []
        self.insights_time_by_symbol = {}

        type_string = Extensions.GetEnumString(type, InsightType)
        direction_string = Extensions.GetEnumString(direction, InsightDirection)

        self.Name = '{}({},{},{}'.format(self.__class__.__name__, type_string, direction_string, strfdelta(period))
        if magnitude is not None:
            self.Name += ',{}'.format(magnitude)
        if confidence is not None:
            self.Name += ',{}'.format(confidence)

        self.Name += ')'


    def update(self, algorithm, data):
        ''' Creates a constant insight for each security as specified via the constructor
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        insights = []

        for security in self.securities:
            # security price could be zero until we get the first data point. e.g. this could happen
            # when adding both forex and equities, we will first get a forex data point
            if security.price != 0 and self.should_emit_insight(algorithm.utc_time, security.symbol):
                insights.append(Insight(security.symbol, self.period, self.type, self.direction, self.magnitude, self.confidence, weight = self.weight))

        return insights


    def on_securities_changed(self, algorithm, changes):
        ''' Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for added in changes.added_securities:
            self.securities.append(added)

        # this will allow the insight to be re-sent when the security re-joins the universe
        for removed in changes.removed_securities:
            if removed in self.securities:
                self.securities.remove(removed)
            if removed.symbol in self.insights_time_by_symbol:
                self.insights_time_by_symbol.pop(removed.symbol)


    def should_emit_insight(self, utc_time, symbol):
        if symbol.is_canonical():
            # canonical futures & options are none tradable
            return False

        generated_time_utc = self.insights_time_by_symbol.get(symbol)

        if generated_time_utc is not None:
            # we previously emitted a insight for this symbol, check it's period to see
            # if we should emit another insight
            if utc_time - generated_time_utc < self.period:
                return False

        # we either haven't emitted a insight for this symbol or the previous
        # insight's period has expired, so emit a new insight now for this symbol
        self.insights_time_by_symbol[symbol] = utc_time
        return True

def strfdelta(tdelta):
    d = tdelta.days
    h, rem = divmod(tdelta.seconds, 3600)
    m, s = divmod(rem, 60)
    return "{}.{:02d}:{:02d}:{:02d}".format(d,h,m,s)
