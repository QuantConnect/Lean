#
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
#

from clr import AddReference
AddReference("QuantConnect.Indicators")

from QuantConnect.Indicators import *
from datetime import datetime
import decimal as d
import unittest

class IndicatorExtensionsTests(unittest.TestCase):
    def test_PipesDataUsingOfFromFirstToSecond(self):
        first = SimpleMovingAverage(2)
        second = Delay(1)

        # this is a configuration step, but returns the reference to the second for method chaining
        third = IndicatorExtensions.Of(second, first)

        data1 = IndicatorDataPoint(datetime.now(), 1)
        data2 = IndicatorDataPoint(datetime.now(), 2)
        data3 = IndicatorDataPoint(datetime.now(), 3)
        data4 = IndicatorDataPoint(datetime.now(), 4)

        # sma has one item
        first.Update(data1)
        self.assertFalse(first.IsReady)
        self.assertEqual(0, second.Current.Value)

        # sma is ready, delay will repeat this value
        first.Update(data2)
        self.assertTrue(first.IsReady)
        self.assertFalse(second.IsReady)
        self.assertEqual(1.5, second.Current.Value)

        # delay is ready, and repeats its first input
        first.Update(data3)
        self.assertTrue(second.IsReady)
        self.assertEqual(1.5, second.Current.Value)

        # now getting the delayed data
        first.Update(data4)
        self.assertEqual(2.5, second.Current.Value)

    def test_PipesDataFirstWeightedBySecond(self):
        period = 4
        value = Identity("Value")
        weight = Identity("Weight")

        third = IndicatorExtensions.WeightedBy(value, weight, period)

        data = range(1, 11)
        window = list(reversed(data))[:period]
        current = sum([ 2 * x * x for x in window ]) / float(sum(window))

        for item in data:
            value.Update(datetime.now(), 2 * item)
            weight.Update(datetime.now(), item)

        self.assertEqual(current, float(third.Current.Value))

    def test_NewDataPushesToDerivedIndicators(self):
        identity = Identity("identity")
        self.sma = SimpleMovingAverage(3)

        identity.Updated += self.identity_updated

        identity.Update(datetime.now(), 1)
        identity.Update(datetime.now(), 2)
        self.assertFalse(self.sma.IsReady)

        identity.Update(datetime.now(), 3)
        self.assertTrue(self.sma.IsReady)
        self.assertEqual(2, self.sma.Current.Value)

    def identity_updated(self, sender, consolidated):
        self.sma.Update(consolidated)

    def test_MultiChainSMA(self):
        identity = Identity("identity")
        delay = Delay(2)

        # create the SMA of the delay of the identity
        sma = IndicatorExtensions.SMA(IndicatorExtensions.Of(delay, identity), 2)

        identity.Update(datetime.now(), 1)
        self.assertTrue(identity.IsReady)
        self.assertFalse(delay.IsReady)
        self.assertFalse(sma.IsReady)

        identity.Update(datetime.now(), 2)
        self.assertTrue(identity.IsReady)
        self.assertFalse(delay.IsReady)
        self.assertFalse(sma.IsReady)

        identity.Update(datetime.now(), 3)
        self.assertTrue(identity.IsReady)
        self.assertTrue(delay.IsReady)
        self.assertFalse(sma.IsReady)

        identity.Update(datetime.now(), 4)
        self.assertTrue(identity.IsReady)
        self.assertTrue(delay.IsReady)
        self.assertTrue(sma.IsReady)

        self.assertEqual(1.5, sma.Current.Value)

    def test_MultiChainEMA(self):
        identity = Identity("identity")
        delay = Delay(2)

        # create the EMA of chained methods
        ema = IndicatorExtensions.EMA(IndicatorExtensions.Of(delay, identity), 2, d.Decimal(1))
            
        identity.Update(datetime.now(), 1)
        self.assertTrue(identity.IsReady)
        self.assertFalse(delay.IsReady)
        self.assertFalse(ema.IsReady)

        identity.Update(datetime.now(), 2)
        self.assertTrue(identity.IsReady)
        self.assertFalse(delay.IsReady)
        self.assertFalse(ema.IsReady)

        identity.Update(datetime.now(), 3)
        self.assertTrue(identity.IsReady)
        self.assertTrue(delay.IsReady)
        self.assertFalse(ema.IsReady)

        identity.Update(datetime.now(), 4)
        self.assertTrue(identity.IsReady)
        self.assertTrue(delay.IsReady)
        self.assertTrue(ema.IsReady)

        self.assertEqual(2, ema.Current.Value)

    def test_MultiChainMAX(self):
        identity = Identity("identity")
        delay = Delay(2)

        # create the MAX of the delay of the identity
        max = IndicatorExtensions.MAX(IndicatorExtensions.Of(delay, identity), 2)

        identity.Update(datetime.now(), 1)
        self.assertTrue(identity.IsReady)
        self.assertFalse(delay.IsReady)
        self.assertFalse(max.IsReady)

        identity.Update(datetime.now(), 2)
        self.assertTrue(identity.IsReady)
        self.assertFalse(delay.IsReady)
        self.assertFalse(max.IsReady)

        identity.Update(datetime.now(), 3)
        self.assertTrue(identity.IsReady)
        self.assertTrue(delay.IsReady)
        self.assertFalse(max.IsReady)

        identity.Update(datetime.now(), 4)
        self.assertTrue(identity.IsReady)
        self.assertTrue(delay.IsReady)
        self.assertTrue(max.IsReady)

        self.assertEqual(2, max.Current.Value)

    def test_MultiChainMIN(self):
        identity = Identity("identity")
        delay = Delay(2)

        # create the MAX of the delay of the identity
        min = IndicatorExtensions.MIN(IndicatorExtensions.Of(delay, identity), 2)

        identity.Update(datetime.now(), 1)
        self.assertTrue(identity.IsReady)
        self.assertFalse(delay.IsReady)
        self.assertFalse(min.IsReady)

        identity.Update(datetime.now(), 2)
        self.assertTrue(identity.IsReady)
        self.assertFalse(delay.IsReady)
        self.assertFalse(min.IsReady)

        identity.Update(datetime.now(), 3)
        self.assertTrue(identity.IsReady)
        self.assertTrue(delay.IsReady)
        self.assertFalse(min.IsReady)

        identity.Update(datetime.now(), 4)
        self.assertTrue(identity.IsReady)
        self.assertTrue(delay.IsReady)
        self.assertTrue(min.IsReady)

        self.assertEqual(1, min.Current.Value)

    def test_PlusAddsLeftAndRightAfterBothUpdated(self):
        left = Identity("left")
        right = Identity("right")
        composite = IndicatorExtensions.Plus(left, right)

        left.Update(datetime.now(), 1)
        right.Update(datetime.now(), 1)
        self.assertEqual(2, composite.Current.Value)

        left.Update(datetime.today(), 2)
        self.assertEqual(2, composite.Current.Value)

        left.Update(datetime.today(), 3)
        self.assertEqual(2, composite.Current.Value)

        right.Update(datetime.today(), 4)
        self.assertEqual(7, composite.Current.Value)

    def test_MinusSubtractsLeftAndRightAfterBothUpdated(self):
        left = Identity("left")
        right = Identity("right")
        composite = IndicatorExtensions.Minus(left, right)

        left.Update(datetime.today(), 1)
        right.Update(datetime.today(), 1)
        self.assertEqual(0, composite.Current.Value)

        left.Update(datetime.today(), 2)
        self.assertEqual(0, composite.Current.Value)

        left.Update(datetime.today(), 3)
        self.assertEqual(0, composite.Current.Value)

        right.Update(datetime.today(), 4)
        self.assertEqual(-1, composite.Current.Value)

    def test_OverDivdesLeftAndRightAfterBothUpdated(self):
        left = Identity("left")
        right = Identity("right")
        composite = IndicatorExtensions.Over(left, right)

        left.Update(datetime.today(), 1)
        right.Update(datetime.today(), 1)
        self.assertEqual(1, composite.Current.Value)

        left.Update(datetime.today(), 2)
        self.assertEqual(1, composite.Current.Value)

        left.Update(datetime.today(), 3)
        self.assertEqual(1, composite.Current.Value)

        right.Update(datetime.today(), 4)
        self.assertEqual(3.0 / 4.0, composite.Current.Value)

    def test_OverHandlesDivideByZero(self):
        left = Identity("left")
        right = Identity("right")
        composite = IndicatorExtensions.Over(left, right)
        self.updatedEventFired = False
        composite.Updated += self.composite_updated

        left.Update(datetime.today(), 1)
        self.assertFalse(self.updatedEventFired)
        right.Update(datetime.today(), 0)
        self.assertFalse(self.updatedEventFired)

        # submitting another update to right won't cause an update without corresponding update to left
        right.Update(datetime.today(), 1)
        self.assertFalse(self.updatedEventFired)
        left.Update(datetime.today(), 1)
        self.assertTrue(self.updatedEventFired)

    def composite_updated(self, sender, consolidated):
        self.updatedEventFired = True

    def test_TimesMultipliesLeftAndRightAfterBothUpdated(self):
        left = Identity("left")
        right = Identity("right")
        composite = IndicatorExtensions.Times(left, right)

        left.Update(datetime.today(), 1)
        right.Update(datetime.today(), 1)
        self.assertEqual(1, composite.Current.Value)

        left.Update(datetime.today(), 2)
        self.assertEqual(1, composite.Current.Value)

        left.Update(datetime.today(), 3)
        self.assertEqual(1, composite.Current.Value)

        right.Update(datetime.today(), 4)
        self.assertEqual(12, composite.Current.Value)


if __name__ == '__main__':
    unittest.main()