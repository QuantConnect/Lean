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

class MarketOnCloseOrderBufferExtendedMarketHoursRegressionAlgorithm(QCAlgorithm):
    '''This regression test is a version of "MarketOnCloseOrderBufferRegressionAlgorithm"
     where we test market-on-close modeling with data from the post market.'''
    validOrderTicket = None
    invalidOrderTicket = None
    validOrderTicketExtendedMarketHours = None

    def Initialize(self):
        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,8)    #Set End Date

        self.AddEquity("SPY", Resolution.Minute, extendedMarketHours = True)

        def mocAtMidNight():
            self.validOrderTicketAtMidnight = self.MarketOnCloseOrder("SPY", 2)

        self.Schedule.On(self.DateRules.Tomorrow, self.TimeRules.Midnight, mocAtMidNight)

        # Modify our submission buffer time to 10 minutes
        MarketOnCloseOrder.SubmissionTimeBuffer = timedelta(minutes=10)

    def OnData(self, data):
        # Test our ability to submit MarketOnCloseOrders
        # Because we set our buffer to 10 minutes, any order placed
        # before 3:50PM should be accepted, any after marked invalid

        # Will not throw an order error and execute
        if self.Time.hour == 15 and self.Time.minute == 49 and not self.validOrderTicket:
            self.validOrderTicket = self.MarketOnCloseOrder("SPY", 2)

        # Will throw an order error and be marked invalid
        if self.Time.hour == 15 and self.Time.minute == 51 and not self.invalidOrderTicket:
            self.invalidOrderTicket = self.MarketOnCloseOrder("SPY", 2)

        # Will not throw an order error and execute
        if self.Time.hour == 16 and self.Time.minute == 48 and not self.validOrderTicketExtendedMarketHours:
            self.validOrderTicketExtendedMarketHours = self.MarketOnCloseOrder("SPY", 2)

    def OnEndOfAlgorithm(self):
        # Set it back to default for other regressions
        MarketOnCloseOrder.SubmissionTimeBuffer = MarketOnCloseOrder.DefaultSubmissionTimeBuffer

        if self.validOrderTicket.Status != OrderStatus.Filled:
            raise Exception("Valid order failed to fill")

        if self.invalidOrderTicket.Status != OrderStatus.Invalid:
            raise Exception("Invalid order was not rejected")

        if self.validOrderTicketExtendedMarketHours.Status != OrderStatus.Filled:
            raise Exception("Valid order during extended market hours failed to fill")
