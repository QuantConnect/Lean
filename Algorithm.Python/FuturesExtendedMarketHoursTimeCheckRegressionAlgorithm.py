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

### <summary>
### This regression algorithm asserts that the regular and extended hours match what is expected from the database for futures.
### </summary>
class FuturesExtendedMarketHoursTimeCheckRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 10, 6)
        self.SetEndDate(2013, 10, 11)

        self._es = self.AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, fillDataForward=True, extendedMarketHours=True)
        self._es.SetFilter(0, 180)

    def OnData(self, slice):
        esIsInRegularHours = self._es.Exchange.Hours.IsOpen(self.Time, False)
        esIsInExtendedHours = not esIsInRegularHours and self._es.Exchange.Hours.IsOpen(self.Time, True)

        timeOfDay = self.Time.time()
        currentTimeIsRegularHours = (timeOfDay >= time(9, 30, 0) and timeOfDay < time(16, 15, 0)) or (timeOfDay >= time(16, 30, 0) and timeOfDay < time(17, 0, 0))
        currentTimeIsExtendedHours = not currentTimeIsRegularHours and (timeOfDay < time(9, 30, 0) or timeOfDay >= time(18, 0, 0))

        if esIsInRegularHours != currentTimeIsRegularHours or esIsInExtendedHours != currentTimeIsExtendedHours:
            raise Exception("At {Time}, {_es.Symbol} is either in regular hours but current time is in extended hours, or viceversa")
