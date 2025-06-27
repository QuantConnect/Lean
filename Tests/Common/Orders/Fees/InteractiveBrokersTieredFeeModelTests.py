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
AddReference("QuantConnect.Orders.MarketOrder")
AddReference("QuantConnect.Orders.Fees.OrderFeeParameters")
AddReference("QuantConnect.Orders.Fees.InteractiveBrokersTieredFeeModel")
AddReference("QuantConnect.Tests.Symbols")

from QuantConnect.Order.MarketOrder import *
from QuantConnect.Orders.Fees.OrderFeeParameters import *
from QuantConnect.Orders.Fees.InteractiveBrokersTieredFeeModel import InteractiveBrokersTieredFeeModel
from QuantConnect.Orders.Tests.Symbols import *
from AlgorithmImports import *
import unittest

class InteractiveBrokersTieredFeeModelTests(unittest.TestCase):
    
    def setUp(self):
        self.model = InteractiveBrokersTieredFeeModel()

    def test_equity_commission_rate_tiers(self):
        self.model.reprocess_rate_schedule(250000, 0, 0, 0, 0)
        self.assertEqual(self.model.equity_commission_rate, 0.0035)

        self.model.reprocess_rate_schedule(5000000, 0, 0, 0, 0)
        self.assertEqual(self.model.equity_commission_rate, 0.002)

        self.model.reprocess_rate_schedule(15000000, 0, 0, 0, 0)
        self.assertEqual(self.model.equity_commission_rate, 0.0015)

        self.model.reprocess_rate_schedule(50000000, 0, 0, 0, 0)
        self.assertEqual(self.model.equity_commission_rate, 0.001)

        self.model.reprocess_rate_schedule(100000000, 0, 0, 0, 0)
        self.assertEqual(self.model.equity_commission_rate, 0.0005)

    def test_get_order_fee_for_equity(self):
        order = MarketOrder(Symbols.SPY, 100, datetime.now())
        fee = self.model.get_order_fee(OrderFeeParameters(order, None))
        self.assertIsNotNone(fee)
        self.assertTrue(fee.amount > 0)

    def test_forex_fee_calculation(self):
        order = MarketOrder(Symbols.EURUSD, 1000, datetime.now())
        security = Forex(Symbols.EURUSD)
        fee = self.model.get_order_fee(OrderFeeParameters(order, security))
        self.assertIsNotNone(fee)
        self.assertTrue(fee.amount > 0)

    def test_crypto_fee_calculation(self):
        order = MarketOrder(Symbols.BTCUSD, 1, datetime.now())
        security = Crypto(Symbols.BTCUSD)
        fee = self.model.get_order_fee(OrderFeeParameters(order, security))
        self.assertIsNotNone(fee)
        self.assertTrue(fee.amount > 0)

    def test_option_fee_calculation(self):
        order = MarketOrder(Symbols.SPY_C_192_Feb19_2016, 5, datetime.now())
        security = Option(Symbols.SPY_C_192_Feb19_2016)
        fee = self.model.get_order_fee(OrderFeeParameters(order, security))
        self.assertIsNotNone(fee)
        self.assertTrue(fee.amount > 0)

    def test_unsupported_security_type(self):
        order = MarketOrder(Symbols.UNKNOWN, 1, datetime.now())
        security = Security(Symbols.UNKNOWN)
        with self.assertRaises(Exception):
            self.model.get_order_fee(OrderFeeParameters(order, security))

    def test_monthly_rolling_tier_change(self):
        order1 = MarketOrder(Symbols.SPY, 300000, datetime.now())
        self.model.get_order_fee(OrderFeeParameters(order1, None))

        order2 = MarketOrder(Symbols.SPY, 3000000, datetime.now())
        fee = self.model.get_order_fee(OrderFeeParameters(order2, None))
        self.assertEqual(fee.amount, 827630.892)  # Example expected fee for Tier 2

if __name__ == '__main__':
    unittest.main()
